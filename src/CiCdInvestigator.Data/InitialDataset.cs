using Microsoft.EntityFrameworkCore;

namespace CiCdInvestigator.Data;

public static class InitialDataset
{
    public static async Task InitializeAsync(InvestigatorContext database, CancellationToken cancellationToken = default)
    {
        await database.Database.MigrateAsync(cancellationToken);
        if (await database.Runs.AnyAsync(x => x.Id == "run-102", cancellationToken)) return;

        database.Pipelines.Add(new Pipeline { Id = "pricing-ci", Name = "Pricing CI", Repository = "mock/pricing" });
        database.Commits.AddRange(
            new Commit { Id = "revision-101", Message = "Add price rounding tests" },
            new Commit { Id = "revision-102", Message = "Simplify price rounding" });
        for (int number = 101; number <= 102; number++)
        {
            bool failed = number == 102;
            string runId = $"run-{number}";
            string jobId = $"job-{number}-test-1";
            database.Runs.Add(new Run
            {
                Id = runId, PipelineId = "pricing-ci", DatasetVersion = "mock-v1", Ref = "refs/heads/main",
                CommitId = $"revision-{number}", StartedAt = new DateTimeOffset(2026, 10, 20, number - 92, 0, 0, TimeSpan.Zero),
                State = ExecutionState.Completed, Outcome = failed ? Outcome.Failed : Outcome.Succeeded,
                Environment = "ubuntu-22.04; dotnet-sdk=10.0.401", Configuration = "Release"
            });
            database.Jobs.Add(new Job
            {
                Id = jobId, RunId = runId, Name = "test", Attempt = 1, State = ExecutionState.Completed,
                Outcome = failed ? Outcome.Failed : Outcome.Succeeded,
                LogsStatus = SourceStatus.Complete, TestsStatus = SourceStatus.Complete
            });
            database.Logs.AddRange(
                new LogEntry { JobId = jobId, Position = 1, Text = "dotnet test --configuration Release" },
                new LogEntry { JobId = jobId, Position = 2, Text = "warning: build cache unavailable; continuing without cache" },
                new LogEntry { JobId = jobId, Position = 3, Text = failed
                    ? "FAIL PriceTests.RoundMidpoint: input 1.005; expected 1.01; actual 1.00"
                    : "PASS PriceTests.RoundMidpoint: input 1.005; expected 1.01; actual 1.01" });
            database.Tests.Add(new TestResult
            {
                Id = $"test-{number}-round", JobId = jobId, Name = "PriceTests.RoundMidpoint",
                Outcome = failed ? Outcome.Failed : Outcome.Succeeded,
                Message = failed ? "Expected: 1.01; Actual: 1.00; Input: 1.005m" : null
            });
            database.Changes.Add(new ChangeSet
            {
                RunId = runId, BaseCommitId = failed ? "revision-101" : null,
                Status = failed ? SourceStatus.Complete : SourceStatus.Unavailable,
                Patch = failed ? "--- a/Price.cs\n+++ b/Price.cs\n@@ -1 +1 @@\n-return Math.Round(amount, 2, MidpointRounding.AwayFromZero);\n+return Math.Round(amount, 2);\n" : null
            });
        }
        await database.SaveChangesAsync(cancellationToken);
    }
}
