using System.Text.Json;
using CiCdInvestigator.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace CiCdInvestigator.Data.Tests;

public sealed class McpEvidenceTests
{
    [Fact]
    [Trait("Category", "Mcp")]
    public async Task PartialLogsAndAttemptsRemainDistinctOverMcp()
    {
        string connection = Environment.GetEnvironmentVariable("INVESTIGATOR_MCP_TEST_SQL")
            ?? throw new InvalidOperationException("Use scripts/Test-McpIntegration.ps1.");
        Assert.Equal("investigator-mcp-test", new SqlConnectionStringBuilder(connection).InitialCatalog);
        string endpoint = Environment.GetEnvironmentVariable("INVESTIGATOR_MCP_TEST_URL")
            ?? throw new InvalidOperationException("Missing isolated MCP URL.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var options = new DbContextOptionsBuilder<InvestigatorContext>().UseSqlServer(connection).Options;
        await using var database = new InvestigatorContext(options);
        // Only the explicitly named test database is changed. Re-running is idempotent.
        var first = await database.Jobs.SingleAsync(x => x.Id == "job-102-test-1", timeout.Token);
        first.LogsStatus = SourceStatus.Truncated;
        if (!await database.Jobs.AnyAsync(x => x.Id == "job-102-test-2", timeout.Token))
        {
            database.Jobs.Add(new Job
            {
                Id = "job-102-test-2", RunId = "run-102", Name = "test", Attempt = 2,
                State = ExecutionState.Completed, Outcome = Outcome.Succeeded,
                LogsStatus = SourceStatus.Complete, TestsStatus = SourceStatus.Complete
            });
            database.Logs.Add(new LogEntry { JobId = "job-102-test-2", Position = 1, Text = "Attempt 2: test passed." });
            database.Tests.Add(new TestResult { Id = "test-102-round-2", JobId = "job-102-test-2", Name = "PriceTests.RoundMidpoint", Outcome = Outcome.Succeeded });
        }
        await database.SaveChangesAsync(timeout.Token);
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Endpoint = new Uri(endpoint), TransportMode = HttpTransportMode.StreamableHttp
        }), cancellationToken: timeout.Token);

        async Task<JsonElement> Call(string tool, string runId, string jobId, int attempt, int? count = null)
        {
            var arguments = new Dictionary<string, object?> { ["runId"] = runId, ["jobId"] = jobId, ["attempt"] = attempt };
            if (count is not null) arguments["count"] = count;
            var response = await client.CallToolAsync(tool, arguments, cancellationToken: timeout.Token);
            Assert.False(response.IsError == true);
            using var document = JsonDocument.Parse(string.Concat(response.Content.OfType<TextContentBlock>().Select(x => x.Text)));
            return document.RootElement.Clone();
        }
        var partial = (await Call("read_job_log", "run-102", first.Id, 1)).GetProperty("data");
        Assert.Equal("Truncated", partial.GetProperty("sourceStatus").GetString());
        Assert.False(partial.GetProperty("hasMore").GetBoolean());
        var identity = partial.GetProperty("identity");
        Assert.Equal("mock-v1", identity.GetProperty("datasetVersion").GetString());
        Assert.Equal("run-102", identity.GetProperty("runId").GetString());
        Assert.Equal(first.Id, identity.GetProperty("jobId").GetString());
        Assert.Equal(1, identity.GetProperty("attempt").GetInt32());
        Assert.Contains("expected 1.01; actual 1.00", partial.GetProperty("lines")[2].GetProperty("text").GetString());
        var bounded = (await Call("read_job_log", "run-102", first.Id, 1, 1)).GetProperty("data");
        Assert.Equal("Truncated", bounded.GetProperty("sourceStatus").GetString());
        Assert.True(bounded.GetProperty("hasMore").GetBoolean());
        var second = (await Call("read_job_log", "run-102", "job-102-test-2", 2)).GetProperty("data");
        Assert.Equal("Complete", second.GetProperty("sourceStatus").GetString());
        Assert.Equal(2, second.GetProperty("identity").GetProperty("attempt").GetInt32());
        Assert.Equal("Attempt 2: test passed.", Assert.Single(second.GetProperty("lines").EnumerateArray()).GetProperty("text").GetString());
        var firstTests = (await Call("get_test_results", "run-102", first.Id, 1)).GetProperty("data");
        var secondTests = (await Call("get_test_results", "run-102", "job-102-test-2", 2)).GetProperty("data");
        Assert.Equal("Failed", Assert.Single(firstTests.GetProperty("tests").EnumerateArray()).GetProperty("outcome").GetString());
        Assert.Equal("Succeeded", Assert.Single(secondTests.GetProperty("tests").EnumerateArray()).GetProperty("outcome").GetString());
        Assert.Equal("NotFound", (await Call("read_job_log", "run-102", first.Id, 2)).GetProperty("status").GetString());
        Assert.Equal("NotFound", (await Call("get_test_results", "run-101", "job-102-test-2", 2)).GetProperty("status").GetString());
    }
}
