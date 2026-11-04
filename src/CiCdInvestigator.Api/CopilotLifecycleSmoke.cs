using System.Diagnostics;
using System.Text.Json;
using GitHub.Copilot;

namespace CiCdInvestigator.Api;

internal static class CopilotLifecycleSmoke
{
    public static async Task RunAsync(string endpoint, string cliPath, string model)
    {
        Console.WriteLine($"Lifecycle smoke API process: {Environment.ProcessId}");
        await using (var client = new CopilotClient(new CopilotClientOptions
        {
            Connection = RuntimeConnection.ForStdio(cliPath),
            Mode = CopilotClientMode.CopilotCli,
            UseLoggedInUser = true
        }))
        {
            string marker = Guid.NewGuid().ToString("N");
            string previousSessionId;
            await using (var session = await client.CreateSessionAsync(CopilotSmoke.CreateSessionConfig(endpoint, model)))
            {
                previousSessionId = session.SessionId;
                await session.SendAndWaitAsync(new MessageOptions
                {
                    Prompt = $"Remember this conversation marker: {marker}. Reply OK."
                }, TimeSpan.FromMinutes(2));
                var recall = await session.SendAndWaitAsync(new MessageOptions
                {
                    Prompt = "Return only the conversation marker I gave you."
                }, TimeSpan.FromMinutes(2));
                if (recall?.Data.Content.Contains(marker, StringComparison.Ordinal) != true)
                    throw new InvalidOperationException("Same-session marker recall failed.");
                Console.WriteLine("PASS: same-session marker recall.");

                var toolStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                int toolCalls = 0;
                int assistantMessages = 0;
                using var subscription = session.On((SessionEvent sessionEvent) =>
                {
                    if (sessionEvent is ToolExecutionStartEvent)
                    {
                        Interlocked.Increment(ref toolCalls);
                        toolStarted.TrySetResult();
                    }
                    if (sessionEvent is AssistantMessageEvent) Interlocked.Increment(ref assistantMessages);
                    if (sessionEvent is SessionIdleEvent) idle.TrySetResult();
                    if (sessionEvent is SessionErrorEvent)
                    {
                        var error = new InvalidOperationException("SDK reported a session error during cancellation smoke.");
                        toolStarted.TrySetException(error);
                        idle.TrySetException(error);
                    }
                });
                await session.SendAsync(new MessageOptions
                {
                    Prompt = "Call cicd-get_run for run-102, then call it again for run-101. Repeat these reads 20 times sequentially, then summarize."
                });
                await toolStarted.Task.WaitAsync(TimeSpan.FromMinutes(2));
                if (idle.Task.IsCompleted) throw new InvalidOperationException("Turn finished before cancellation could be tested.");
                var stopwatch = Stopwatch.StartNew();
                await session.AbortAsync();
                await idle.Task.WaitAsync(TimeSpan.FromSeconds(15));
                stopwatch.Stop();
                int callsAfterAbort = Volatile.Read(ref toolCalls);
                int messagesAfterAbort = Volatile.Read(ref assistantMessages);
                await Task.Delay(TimeSpan.FromSeconds(3));
                if (Volatile.Read(ref toolCalls) != callsAfterAbort || Volatile.Read(ref assistantMessages) != messagesAfterAbort)
                    throw new InvalidOperationException("Work continued after cancellation reached idle.");
                Console.WriteLine($"PASS: AbortAsync reached idle in {stopwatch.ElapsedMilliseconds} ms; {callsAfterAbort} tool starts; no new calls/messages over 3 seconds.");
            }
            await using (var fresh = await client.CreateSessionAsync(CopilotSmoke.CreateSessionConfig(endpoint, model)))
            {
                if (fresh.SessionId == previousSessionId) throw new InvalidOperationException("Session ID was reused.");
                var events = await fresh.GetEventsAsync();
                if (events.Any(sessionEvent => JsonSerializer.Serialize(sessionEvent, sessionEvent.GetType()).Contains(marker, StringComparison.Ordinal)))
                    throw new InvalidOperationException("Previous marker appeared in new session history.");
                var answer = await fresh.SendAndWaitAsync(new MessageOptions
                {
                    Prompt = "What conversation marker did I give you earlier? Return the exact marker if present in this conversation; otherwise reply UNKNOWN."
                }, TimeSpan.FromMinutes(2));
                if (answer?.Data.Content.Trim() != "UNKNOWN")
                    throw new InvalidOperationException("New session did not report missing context.");
                Console.WriteLine("PASS: distinct fresh session, no prior marker in history, answer UNKNOWN.");
            }
        }
        Console.WriteLine("PASS: sessions and SDK client disposed normally. CLI-managed files were left untouched.");
    }
}
