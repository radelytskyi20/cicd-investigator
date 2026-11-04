using System.Threading.Channels;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Connections;
using CiCdInvestigator.Api;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

if (args.Length is < 2 or > 3 || (args.Length == 3 && args[2] != "--follow-up-only")) throw new ArgumentException("Supply MCP endpoint, installed CLI path and optional --follow-up-only.");
bool followUpOnly = args.Length == 3;
string? fixtureConnection = Environment.GetEnvironmentVariable("INVESTIGATOR_REPORT_TEST_SQL");
if (fixtureConnection is not null) await CiCdInvestigator.Agent.Smoke.InsufficientDataFixture.SeedAsync(fixtureConnection);
await using var app = ApiApplication.Build([
    "--urls", "http://127.0.0.1:0", "--Mcp:Endpoint", args[0], "--Copilot:CliPath", args[1],
    "--Logging:LogLevel:Default", "Warning", "--Logging:LogLevel:CiCdInvestigator.Api.InvestigationAgentService", "Information"
]);
await app.StartAsync();
string endpoint = app.Urls.Single() + "/investigation";
Console.WriteLine($"Agent smoke process: {Environment.ProcessId}");
ClientWebSocket? socket = null;
await using var connection = new HubConnectionBuilder().WithUrl(endpoint, options =>
{
    options.Transports = HttpTransportType.WebSockets;
    options.SkipNegotiation = true;
    options.WebSocketFactory = async (context, cancellationToken) =>
    {
        socket = new ClientWebSocket();
        await socket.ConnectAsync(context.Uri, cancellationToken);
        return socket;
    };
}).Build();
var events = Channel.CreateUnbounded<InvestigationEvent>();
using var subscription = connection.On<InvestigationEvent>("investigationEvent", notification => { if (notification.Type == "error") Console.WriteLine(notification.Text); events.Writer.TryWrite(notification); });
await connection.StartAsync();

async Task<List<InvestigationEvent>> FinishAsync(Task invocation)
{
    await invocation.WaitAsync(TimeSpan.FromMinutes(3));
    var received = new List<InvestigationEvent>();
    while (true)
    {
        var notification = await events.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromMinutes(3));
        received.Add(notification);
        if (notification.Type == "ended") break;
    }

    return received;
}

async Task<List<InvestigationEvent>> ReportWithExplicitRetryAsync(Task invocation)
{
    var result = await FinishAsync(invocation);
    Console.WriteLine($"Report request observed {result.Count(item => item.Type == "toolStarted")} tool starts.");
    var failure = result.FirstOrDefault(item => item.Type == "error" && item.Text!.StartsWith("Report validation failed:", StringComparison.Ordinal));
    if (failure is null) return result;
    Check(result.All(item => item.Type != "answerReady"), "Invalid report emits no ready result.");
    Console.WriteLine("SMOKE CLIENT: submitting one explicit corrective follow-up; the application does not retry automatically.");
    return await FinishAsync(connection.InvokeAsync("FollowUp", failure.Text + " Correct the report using only exact values from the MCP results already read. Return the full compact JSON report."));
}

async Task<List<InvestigationEvent>> CheckFreshConnectionAsync()
{
    // Disconnect cleanup includes CLI process disposal; wait for that bounded transition.
    for (int attempt = 0; attempt < 80; attempt++)
    {
        try { return await FinishAsync(connection.InvokeAsync("FollowUp", "What was the previous run?")); }
        catch (HubException exception) when (exception.Message.Contains("still running", StringComparison.Ordinal))
        {
            await Task.Delay(250);
        }
    }
    throw new TimeoutException("Disconnect cleanup did not finish.");
}

async Task WaitForToolAsync()
{
    while (true)
    {
        var notification = await events.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromMinutes(2));
        if (notification.Type == "toolStarted") return;
        if (notification.Type is "ended" or "error") throw new InvalidOperationException("Turn ended before a tool started.");
    }
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    Console.WriteLine("PASS: " + message);
}

var missing = await FinishAsync(connection.InvokeAsync("FollowUp", "Hello"));
Check(missing.Any(item => item.Type == "error") && missing.Last().Text == "error", "Follow-up before Start reports an error.");
var initial = await ReportWithExplicitRetryAsync(connection.InvokeAsync("Start", new InvestigationRequest("run-102", "Explain the failure, compare with the preceding successful run and investigate the available change.")));
var initialReport = initial.SingleOrDefault(item => item.Type == "answerReady")?.Report;
if (initialReport is null) throw new InvalidOperationException(string.Join("; ", initial.Select(item => item.Text)));
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(initialReport));
Check(initialReport.Report.Outcome == "supported" && initialReport.Report.Evidence.Any(item => item.Tool == "get_changes") && initialReport.ToolCallCount > 0, "S01 produces a validated report with change evidence.");
var followUp = await ReportWithExplicitRetryAsync(connection.InvokeAsync("FollowUp", "Could the cache warning explain the test failure? Update the compact report using earlier evidence where sufficient."));
Check(followUp.Any(item => item.Type == "answerReady"), "Follow-up returns an updated validated report.");
var followUpReport = followUp.Single(item => item.Type == "answerReady").Report!;
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(followUpReport));
string explanation = string.Join(" ", new[] { followUpReport.Report.Summary, followUpReport.Report.Limitations }
    .Concat(followUpReport.Report.Hypotheses.Select(hypothesis => hypothesis.Explanation)));
Check(explanation.Contains("cache", StringComparison.OrdinalIgnoreCase)
    && explanation.Contains("round", StringComparison.OrdinalIgnoreCase), "Follow-up explicitly addresses cache and rounding explanations.");
Check(followUpReport.Report.Evidence.Any(evidence => evidence.Quote.Contains("cache", StringComparison.OrdinalIgnoreCase))
    && followUpReport.Report.Evidence.Any(evidence => evidence.Tool == "get_changes")
    && followUpReport.Report.Evidence.Any(evidence => evidence.Tool is "get_test_results" or "read_job_log" && evidence.Quote.Contains("1.00", StringComparison.Ordinal)),
    "Follow-up includes validated warning, change and failure evidence.");
int reusedEvidence = followUpReport.Report.Evidence.Intersect(initialReport.Report.Evidence).Count();
Console.WriteLine($"Follow-up: {reusedEvidence} exact citations reused; {followUpReport.ToolCallCount} calls in final turn; duration {followUpReport.DurationMs} ms.");
Console.WriteLine("Semantic review still required: check whether the explanation weakens the cache hypothesis and labels uninspected versus unavailable sources accurately.");
if (followUpOnly)
{
    await connection.StopAsync();
    await app.StopAsync();
    Console.WriteLine("PASS: focused S01 follow-up check completed; unrelated lifecycle scenarios were not rerun.");
    return;
}
var fresh = await FinishAsync(connection.InvokeAsync("Start", new InvestigationRequest("run-101", "Explain this run.")));
Check(fresh.All(item => item.Type != "answerReady") && fresh.Any(item => item.Type == "error"), "Successful target is not accepted as a failed investigation.");
if (fixtureConnection is not null)
{
    var insufficient = await ReportWithExplicitRetryAsync(connection.InvokeAsync("Start", new InvestigationRequest("run-insufficient", "Investigate this failure using available evidence.")));
    var unknown = insufficient.SingleOrDefault(item => item.Type == "answerReady")?.Report;
    if (unknown is null) throw new InvalidOperationException(string.Join("; ", insufficient.Select(item => item.Text)));
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(unknown));
    Check(unknown.Report.Outcome == "unknown" && unknown.Report.BaselineId is null
        && unknown.Report.Evidence.Any(item => item.Completeness is "Truncated" or "Unavailable"), "Insufficient data produces a validated unknown-cause report.");
}
if (fixtureConnection is null)
{
var restarted = await ReportWithExplicitRetryAsync(connection.InvokeAsync("Start", new InvestigationRequest("run-102", "Investigate the failure.")));
Check(restarted.Any(item => item.Type == "answerReady"), "Fresh investigation collects its own valid evidence.");
}
const string longPrompt = "Call cicd-get_run for run-102 then run-101. Repeat these reads 30 times sequentially before answering.";
Task running = connection.InvokeAsync("FollowUp", longPrompt);
await WaitForToolAsync();
try
{
    await connection.InvokeAsync("FollowUp", "Overlapping request");
    throw new InvalidOperationException("Overlapping request was not rejected.");
}
catch (HubException) { Console.WriteLine("PASS: overlapping request rejected without a queue."); }
await connection.InvokeAsync("Stop").WaitAsync(TimeSpan.FromSeconds(30));
var cancelled = await FinishAsync(running);
Check(cancelled.Last().Text == "cancelled" && cancelled.All(item => item.Type != "answerReady"), "Stop reports cancellation without a final answer.");
await Task.Delay(1000);
Check(!events.Reader.TryRead(out _), "No events after completed Stop observation.");
await connection.InvokeAsync("Stop");
var continued = await ReportWithExplicitRetryAsync(connection.InvokeAsync("FollowUp", "Update the compact report using the evidence already read."));
Check(continued.Any(item => item.Type == "answerReady"), "Explicit follow-up after Stop continues the conversation.");

Task disconnectTurn = connection.InvokeAsync("FollowUp", longPrompt);
await WaitForToolAsync();
var disconnectTimer = System.Diagnostics.Stopwatch.StartNew();
await connection.StopAsync();
try { await disconnectTurn; } catch (Exception exception) when (exception is OperationCanceledException or IOException or HubException or WebSocketException) { }
await Task.Delay(1500);
while (events.Reader.TryRead(out _)) { }
await connection.StartAsync();
var afterDisconnect = await CheckFreshConnectionAsync();
Check(afterDisconnect.Any(item => item.Type == "error" && item.Text == "Start an investigation first."), "Disconnect clears the current session.");
Console.WriteLine($"Disconnect/reconnect check completed in {disconnectTimer.ElapsedMilliseconds} ms including a 1500 ms observation wait.");
Task lostConnectionTurn = connection.InvokeAsync("Start", new InvestigationRequest("run-102", longPrompt));
await WaitForToolAsync();
var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
connection.Closed += exception => { connectionClosed.TrySetResult(); return Task.CompletedTask; };
socket!.Abort();
await connectionClosed.Task.WaitAsync(TimeSpan.FromSeconds(15));
try { await lostConnectionTurn; } catch (Exception exception) when (exception is OperationCanceledException or IOException or HubException or WebSocketException) { }
await Task.Delay(1500);
while (events.Reader.TryRead(out _)) { }
await connection.StartAsync();
var afterLoss = await CheckFreshConnectionAsync();
Check(afterLoss.Any(item => item.Type == "error" && item.Text == "Start an investigation first."), "Abrupt WebSocket loss clears the session without automatic reconnect.");
Task shutdownTurn = connection.InvokeAsync("Start", new InvestigationRequest("run-102", longPrompt));
await WaitForToolAsync();
await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(30));
try { await shutdownTurn; } catch (Exception exception) when (exception is OperationCanceledException or IOException or HubException or WebSocketException) { }
Console.WriteLine("PASS: host shutdown completed during active SDK work.");
