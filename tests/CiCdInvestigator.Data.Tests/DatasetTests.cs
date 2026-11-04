using CiCdInvestigator.Data;
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
}
