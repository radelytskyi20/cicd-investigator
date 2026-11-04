using CiCdInvestigator.Data;
using CiCdInvestigator.Data.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CiCdInvestigator.Data.Tests;

public sealed class DatasetTests
{
    [Fact]
    public async Task SqlDatasetPreservesEvidenceAndRepeatedInitialization()
    {
        string connection = Environment.GetEnvironmentVariable("INVESTIGATOR_TEST_SQL")
            ?? throw new InvalidOperationException("Set INVESTIGATOR_TEST_SQL to a disposable SQL Server database.");
        var options = new DbContextOptionsBuilder<InvestigatorContext>().UseSqlServer(connection).Options;
        await using (var database = new InvestigatorContext(options))
        {
            await InitialDataset.InitializeAsync(database);
        }
        await using (var database = new InvestigatorContext(options))
        {
            await InitialDataset.InitializeAsync(database);
            Assert.Equal(2, await database.Runs.CountAsync());
            Assert.Equal(2, await database.Jobs.CountAsync());
            Assert.Equal(6, await database.Logs.CountAsync());
            Assert.Equal(2, await database.Tests.CountAsync());
            var runs = await database.Runs.OrderBy(x => x.StartedAt).ToListAsync();
            Assert.Equal(Outcome.Succeeded, runs[0].Outcome);
            Assert.Equal(Outcome.Failed, runs[1].Outcome);
            Assert.All(runs, x => Assert.Equal(ExecutionState.Completed, x.State));
            Assert.Equal(runs[0].PipelineId, runs[1].PipelineId);
            Assert.Equal(runs[0].Ref, runs[1].Ref);
            var evidence = await (from log in database.Logs
                                  join job in database.Jobs on log.JobId equals job.Id
                                  join run in database.Runs on job.RunId equals run.Id
                                  where run.Id == "run-102" && job.Attempt == 1
                                  orderby log.Position
                                  select new { log.Position, log.Text, run.DatasetVersion }).ToListAsync();
            Assert.Equal(new[] { 1, 2, 3 }, evidence.Select(x => x.Position));
            Assert.Contains("expected 1.01; actual 1.00", evidence[2].Text);
            Assert.All(evidence, x => Assert.Equal("mock-v1", x.DatasetVersion));
            var test = await database.Tests.SingleAsync(x => x.JobId == "job-102-test-1");
            Assert.Equal(Outcome.Failed, test.Outcome);
            Assert.Contains("Expected: 1.01", test.Message);
            var changes = await database.Changes.SingleAsync(x => x.RunId == "run-102");
            Assert.Equal("revision-101", changes.BaseCommitId);
            Assert.Contains("-return Math.Round(amount, 2, MidpointRounding.AwayFromZero);", changes.Patch);
            Assert.Equal(1.01m, Math.Round(1.005m, 2, MidpointRounding.AwayFromZero));
            Assert.Equal(1.00m, Math.Round(1.005m, 2));
            var missing = await database.Changes.SingleAsync(x => x.RunId == "run-101");
            Assert.Equal(SourceStatus.Unavailable, missing.Status);
            Assert.Null(missing.Patch);

            // Exercise source-state round trips without expanding the initial fixture.
            var baselineJob = await database.Jobs.SingleAsync(x => x.Id == "job-101-test-1");
            foreach (var status in new[] { SourceStatus.Empty, SourceStatus.Unavailable, SourceStatus.Truncated, SourceStatus.Complete })
            {
                baselineJob.LogsStatus = status;
                await database.SaveChangesAsync();
                await database.Entry(baselineJob).ReloadAsync();
                Assert.Equal(status, baselineJob.LogsStatus);
            }
        }
    }
    [Fact]
    public async Task QueriesReturnScopedEvidenceAndQualifiedBaseline()
    {
        string connection = Environment.GetEnvironmentVariable("INVESTIGATOR_TEST_SQL")
            ?? throw new InvalidOperationException("Set INVESTIGATOR_TEST_SQL to a disposable SQL Server database.");
        var options = new DbContextOptionsBuilder<InvestigatorContext>().UseSqlServer(connection).Options;
        await using var database = new InvestigatorContext(options);
        await InitialDataset.InitializeAsync(database);
        var queries = new InvestigationQueries(database);
        var runs = await queries.ListRunsAsync("pricing-ci", "refs/heads/main", Outcome.Failed);
        Assert.Equal("run-102", Assert.Single(runs.Data!).Id);
        Assert.Empty((await queries.ListRunsAsync("absent")).Data!);
        Assert.Equal(QueryStatus.InvalidInput, (await queries.ListRunsAsync(outcome: (Outcome)99)).Status);
        Assert.Equal(QueryStatus.NotFound, (await queries.GetRunAsync("absent")).Status);
        Assert.Equal(QueryStatus.InvalidInput, (await queries.GetRunAsync(" ")).Status);
        Assert.Equal(1, Assert.Single((await queries.GetRunAsync("run-102")).Data!.Jobs).Attempt);
        var log = (await queries.ReadLogAsync("run-102", "job-102-test-1", 1, count: 2)).Data!;
        Assert.Equal(SourceStatus.Complete, log.SourceStatus);
        Assert.True(log.HasMore);
        Assert.Equal(2, log.LastLine);
        Assert.Equal("mock-v1", log.Identity.DatasetVersion);
        Assert.Equal(QueryStatus.NotFound, (await queries.ReadLogAsync("run-101", "job-102-test-1", 1)).Status);
        Assert.Equal(QueryStatus.NotFound, (await queries.GetTestsAsync("run-102", "job-102-test-1", 2)).Status);
        Assert.Equal(QueryStatus.InvalidInput, (await queries.ReadLogAsync("run-102", "job-102-test-1", 1, count: 201)).Status);
        Assert.Empty((await queries.ReadLogAsync("run-102", "job-102-test-1", 1, start: 99)).Data!.Lines);
        var tail = (await queries.ReadLogAsync("run-102", "job-102-test-1", 1, start: 3)).Data!;
        Assert.Contains("expected 1.01; actual 1.00", Assert.Single(tail.Lines).Text);
        Assert.False(tail.HasMore);
        Assert.Equal(Outcome.Failed, Assert.Single((await queries.GetTestsAsync("run-102", "job-102-test-1", 1)).Data!.Tests).Outcome);
        var changes = (await queries.GetChangesAsync("run-102")).Data!;
        Assert.Equal("revision-101", changes.BaseCommitId);
        Assert.Contains("MidpointRounding.AwayFromZero", changes.Patch);
        Assert.Equal(SourceStatus.Unavailable, (await queries.GetChangesAsync("run-101")).Data!.SourceStatus);
        var baseline = (await queries.FindBaselineAsync("run-102")).Data!;
        Assert.Equal("run-101", baseline.Candidate!.Id);
        Assert.Empty(baseline.Limitations);
        Assert.Null((await queries.FindBaselineAsync("run-101")).Data!.Candidate);
        Assert.Equal(QueryStatus.NotFound, (await queries.FindBaselineAsync("absent")).Status);
        Assert.Empty(database.ChangeTracker.Entries());

        // Test-local variations roll back; the demonstration fixture remains unchanged.
        await using var transaction = await database.Database.BeginTransactionAsync();
        var job = await database.Jobs.SingleAsync(x => x.Id == "job-102-test-1");
        job.LogsStatus = SourceStatus.Truncated;
        job.TestsStatus = SourceStatus.Unavailable;
        database.Tests.RemoveRange(await database.Tests.Where(x => x.JobId == job.Id).ToListAsync());
        var predecessor = await database.Runs.SingleAsync(x => x.Id == "run-101");
        predecessor.Environment = "different-sdk";
        predecessor.Configuration = null;
        var predecessorJob = await database.Jobs.SingleAsync(x => x.Id == "job-101-test-1");
        predecessorJob.Name = "different-job";
        var storedChanges = await database.Changes.SingleAsync(x => x.RunId == "run-101");
        storedChanges.Status = SourceStatus.Empty;
        storedChanges.Patch = "";
        await database.SaveChangesAsync();
        var partial = (await queries.ReadLogAsync("run-102", job.Id, 1)).Data!;
        Assert.Equal(SourceStatus.Truncated, partial.SourceStatus);
        Assert.False(partial.HasMore);
        var missingTests = (await queries.GetTestsAsync("run-102", job.Id, 1)).Data!;
        Assert.Equal(SourceStatus.Unavailable, missingTests.SourceStatus);
        Assert.Empty(missingTests.Tests);
        Assert.Equal(SourceStatus.Empty, (await queries.GetChangesAsync("run-101")).Data!.SourceStatus);
        var limited = (await queries.FindBaselineAsync("run-102")).Data!;
        Assert.Equal("run-101", limited.Candidate!.Id);
        Assert.Contains("Environment/SDK differs.", limited.Limitations);
        Assert.Contains("Configuration is unknown for at least one run.", limited.Limitations);
        Assert.Contains("Job sets differ.", limited.Limitations);
        await transaction.RollbackAsync();
    }
}
