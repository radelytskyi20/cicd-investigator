using System.Threading.Channels;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Connections;
using CiCdInvestigator.Api;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

if (args.Length != 2) throw new ArgumentException("Supply MCP endpoint and installed CLI path.");
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
using var subscription = connection.On<InvestigationEvent>("investigationEvent", notification => events.Writer.TryWrite(notification));
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

string marker = Guid.NewGuid().ToString("N");
var missing = await FinishAsync(connection.InvokeAsync("FollowUp", "Hello"));
Check(missing.Any(item => item.Type == "error") && missing.Last().Text == "error", "Follow-up before Start reports an error.");
var initial = await FinishAsync(connection.InvokeAsync("Start", new InvestigationRequest("run-102", $"Read the run and state its revision. Remember conversation marker {marker}.")));
Check(initial.Any(item => item.Type == "toolStarted") && initial.Any(item => item.Type == "toolCompleted") && initial.Any(item => item.Type == "answer" && item.Text!.Contains("revision-102")), "Real SignalR/SDK/MCP start returns source-backed revision.");
var followUp = await FinishAsync(connection.InvokeAsync("FollowUp", "Without calling tools, return the conversation marker and the run revision you already read."));
Check(followUp.Any(item => item.Type == "answer" && item.Text!.Contains(marker) && item.Text.Contains("revision-102")) && followUp.All(item => item.Type != "toolStarted"), "Follow-up recalls message and previous tool result without new reads.");
var fresh = await FinishAsync(connection.InvokeAsync("Start", new InvestigationRequest("run-102", "Read the run. State whether a conversation marker was provided in this conversation. If not, include UNKNOWN.")));
Check(fresh.Any(item => item.Type == "answer" && item.Text!.Contains("UNKNOWN") && !item.Text.Contains(marker)), "Next Start has fresh conversation context.");

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
Check(cancelled.Last().Text == "cancelled" && cancelled.All(item => item.Type != "answer"), "Stop reports cancellation without a final answer.");
await Task.Delay(1000);
Check(!events.Reader.TryRead(out _), "No events after completed Stop observation.");
await connection.InvokeAsync("Stop");
var continued = await FinishAsync(connection.InvokeAsync("FollowUp", "Without tools, state the run revision you read earlier."));
Check(continued.Any(item => item.Type == "answer" && item.Text!.Contains("revision-102")), "Explicit follow-up after Stop continues the conversation.");

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
