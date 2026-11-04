using GitHub.Copilot;
using System.Text.Json;

namespace CiCdInvestigator.Api;

internal static class CopilotSmoke
{
    public static async Task RunAsync(string endpoint, string cliPath, string model)
    {
        await using var client = new CopilotClient(new CopilotClientOptions
        {
            Connection = RuntimeConnection.ForStdio(cliPath),
            Mode = CopilotClientMode.CopilotCli,
            UseLoggedInUser = true
        });
        await using var session = await client.CreateSessionAsync(CreateSessionConfig(endpoint, model));
        bool toolStarted = false;
        bool toolCompleted = false;
        bool idle = false;
        using var subscription = session.On((SessionEvent sessionEvent) =>
        {
            switch (sessionEvent)
            {
                case ToolExecutionStartEvent started:
                    toolStarted = started.Data.ToolName == "cicd-get_run";
                    Console.WriteLine($"Tool started: {started.Data.ToolName}; call: {started.Data.ToolCallId}");
                    break;
                case ToolExecutionCompleteEvent completed:
                    string result = JsonSerializer.Serialize(completed.Data.Result);
                    toolCompleted = result.Contains("run-102", StringComparison.Ordinal) && result.Contains("revision-102", StringComparison.Ordinal);
                    Console.WriteLine($"Tool completed; source result available: {toolCompleted}");
                    break;
                case SessionIdleEvent:
                    idle = true;
                    break;
            }
        });
        var answer = await session.SendAndWaitAsync(new MessageOptions
        {
            Prompt = "Call cicd-get_run for run-102. Based only on its returned data, state the run ID, outcome and revision in one sentence."
        }, TimeSpan.FromMinutes(2));
        Console.WriteLine(answer?.Data.Content);
        if (!toolStarted || !toolCompleted || !idle || (answer?.Data.Content is not string content || !content.Contains("run-102") || !content.Contains("revision-102") || !content.Contains("fail", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Real SDK/MCP smoke did not satisfy all checks.");
        }
        Console.WriteLine("PASS: real MCP call, source payload, final answer and session.idle observed.");
    }
    internal static SessionConfig CreateSessionConfig(string endpoint, string model) =>
        new SessionConfig
        {
            Model = model,
            AvailableTools = ["mcp:cicd-get_run"],
            EnableSessionStore = false,
            EnableConfigDiscovery = false,
            EnableSkills = false,
            Memory = new MemoryConfiguration { Enabled = false },
            OnPermissionRequest = PermissionHandler.ApproveAll,
            McpServers = new Dictionary<string, McpServerConfig>
            {
                ["cicd"] = new McpHttpServerConfig { Url = endpoint, Tools = ["get_run"] }
            }
        };
}
