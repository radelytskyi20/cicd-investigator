using CiCdInvestigator.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CiCdInvestigator.Agent.Smoke;

internal static class InsufficientDataFixture
{
    public static async Task SeedAsync(string connection)
    {
        if (new SqlConnectionStringBuilder(connection).InitialCatalog != "investigator-report-test")
            throw new InvalidOperationException("Report fixtures require investigator-report-test.");
        await using var database = new InvestigatorContext(new DbContextOptionsBuilder<InvestigatorContext>().UseSqlServer(connection).Options);
        if (await database.Runs.AnyAsync(run => run.Id == "run-insufficient")) return;
        database.Commits.Add(new Commit { Id = "revision-insufficient", Message = "Synthetic test fixture" });
        database.Runs.Add(new Run
        {
            Id = "run-insufficient", PipelineId = "pricing-ci", DatasetVersion = "report-test-v1", Ref = "refs/heads/insufficient",
            CommitId = "revision-insufficient", StartedAt = new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeSpan.Zero),
            State = ExecutionState.Completed, Outcome = Outcome.Failed
        });
        database.Jobs.Add(new Job
        {
            Id = "job-insufficient", RunId = "run-insufficient", Name = "test", Attempt = 1,
            State = ExecutionState.Completed, Outcome = Outcome.Failed,
            LogsStatus = SourceStatus.Truncated, TestsStatus = SourceStatus.Unavailable
        });
        database.Logs.Add(new LogEntry { JobId = "job-insufficient", Position = 1, Text = "Job started; remaining output unavailable." });
        await database.SaveChangesAsync();
    }
}
