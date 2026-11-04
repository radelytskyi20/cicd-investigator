using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

if (args.Length != 1) throw new ArgumentException("Pass the running host's /mcp URL.");
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
await using var client = await McpClient.CreateAsync(new HttpClientTransport(new()
{
    Endpoint = new Uri(args[0]), TransportMode = HttpTransportMode.StreamableHttp
}), cancellationToken: timeout.Token);
var tools = await client.ListToolsAsync(cancellationToken: timeout.Token);
string[] expected = ["find_baseline", "get_changes", "get_run", "get_test_results", "list_runs", "read_job_log"];
if (!tools.Select(x => x.Name).Order().SequenceEqual(expected)) throw new InvalidOperationException("Unexpected tool catalog.");

async Task<JsonElement> Call(string name, Dictionary<string, object?> arguments)
{
    var response = await client.CallToolAsync(name, arguments, cancellationToken: timeout.Token);
    if (response.IsError == true) throw new InvalidOperationException($"MCP execution error: {name}");
    string text = string.Concat(response.Content.OfType<TextContentBlock>().Select(x => x.Text));
    using var document = JsonDocument.Parse(text);
    return document.RootElement.Clone();
}
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
var runs = await Call("list_runs", new() { ["outcome"] = "Failed" });
Check(runs.GetProperty("status").GetString() == "Ok" && runs.GetProperty("data")[0].GetProperty("id").GetString() == "run-102", "Run listing failed.");
var run = await Call("get_run", new() { ["runId"] = "run-102" });
Check(run.GetProperty("data").GetProperty("jobs")[0].GetProperty("attempt").GetInt32() == 1, "Job identity missing.");
var log = await Call("read_job_log", new() { ["runId"] = "run-102", ["jobId"] = "job-102-test-1", ["attempt"] = 1, ["start"] = 3 });
Check(log.GetProperty("data").GetProperty("lines")[0].GetProperty("text").GetString()!.Contains("expected 1.01; actual 1.00"), "Log evidence differs.");
var tests = await Call("get_test_results", new() { ["runId"] = "run-102", ["jobId"] = "job-102-test-1", ["attempt"] = 1 });
Check(tests.GetProperty("data").GetProperty("tests")[0].GetProperty("outcome").GetString() == "Failed", "Test outcome differs.");
var changes = await Call("get_changes", new() { ["runId"] = "run-102" });
Check(changes.GetProperty("data").GetProperty("baseCommitId").GetString() == "revision-101", "Diff scope differs.");
var baseline = await Call("find_baseline", new() { ["runId"] = "run-102" });
Check(baseline.GetProperty("data").GetProperty("candidate").GetProperty("id").GetString() == "run-101", "Baseline differs.");
var unknown = await Call("get_run", new() { ["runId"] = "missing" });
Check(unknown.GetProperty("status").GetString() == "NotFound", "Unknown run was not distinguished.");
var invalid = await Call("read_job_log", new() { ["runId"] = "run-102", ["jobId"] = "job-102-test-1", ["attempt"] = 1, ["count"] = 201 });
Check(invalid.GetProperty("status").GetString() == "InvalidInput", "Invalid range was not distinguished.");
var unavailable = await Call("get_changes", new() { ["runId"] = "run-101" });
Check(unavailable.GetProperty("data").GetProperty("sourceStatus").GetString() == "Unavailable", "Unavailable source was not distinguished.");
Check((await Call("get_run", new() { ["runId"] = "run-102" })).GetProperty("status").GetString() == "Ok", "Host did not recover after bad inputs.");
Console.WriteLine("PASS: six MCP tools discovered and called; S01 evidence, baseline, unknown ID, invalid range and unavailable source verified.");
