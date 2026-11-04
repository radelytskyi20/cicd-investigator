using Microsoft.EntityFrameworkCore;

namespace CiCdInvestigator.Data;

public static class AdditionalDataset
{
    public static async Task InitializeAsync(InvestigatorContext database, CancellationToken cancellationToken)
    {
        if (!await database.Runs.AnyAsync(run => run.Id == "run-202", cancellationToken))
        {
            database.Commits.Add(new Commit { Id = "revision-201", Message = "Add pricing rules package reference" });
            for (int number = 201; number <= 202; number++)
            {
                bool failed = number == 202;
                string runId = $"run-{number}";
                string restoreId = $"job-{number}-restore-1";
                string testId = $"job-{number}-test-1";
                database.Runs.Add(new Run
                {
                    Id = runId, PipelineId = "pricing-ci", DatasetVersion = "mock-extra-v1",
                    Ref = "refs/heads/package-restore", CommitId = "revision-201",
                    StartedAt = new DateTimeOffset(2026, 10, 21, number - 192, 0, 0, TimeSpan.Zero),
                    State = ExecutionState.Completed, Outcome = failed ? Outcome.Failed : Outcome.Succeeded,
                    Environment = "ubuntu-22.04; dotnet-sdk=10.0.401", Configuration = "Release"
                });
                database.Jobs.AddRange(
                    new Job { Id = restoreId, RunId = runId, Name = "restore", Attempt = 1, State = ExecutionState.Completed,
                        Outcome = failed ? Outcome.Failed : Outcome.Succeeded, LogsStatus = SourceStatus.Complete, TestsStatus = SourceStatus.Empty },
                    new Job { Id = testId, RunId = runId, Name = "test", Attempt = 1, State = ExecutionState.Completed,
                        Outcome = failed ? Outcome.Skipped : Outcome.Succeeded, LogsStatus = SourceStatus.Complete, TestsStatus = failed ? SourceStatus.Empty : SourceStatus.Complete });
                database.Logs.AddRange(
                    new LogEntry { JobId = restoreId, Position = 1, Text = "dotnet restore Pricing.sln --source https://packages.example.invalid/mock-feed/v3/index.json" },
                    new LogEntry { JobId = restoreId, Position = 2, Text = failed
                        ? "error NU1101: Unable to find package Contoso.Pricing.Rules. No packages exist with this id in source(s): mock-feed"
                        : "Restored Pricing.csproj; Contoso.Pricing.Rules 1.2.0 resolved from mock-feed." },
                    new LogEntry { JobId = testId, Position = 1, Text = failed
                        ? "Job skipped: dependency restore failed. dotnet test was not started."
                        : "PASS PriceTests.ApplyRules: expected 12.50; actual 12.50" });
                if (failed)
                    database.Logs.Add(new LogEntry { JobId = restoreId, Position = 3, Text = "Restore failed. Process exited with code 1." });
                else
                    database.Tests.Add(new TestResult { Id = "test-201-rules", JobId = testId, Name = "PriceTests.ApplyRules", Outcome = Outcome.Succeeded });
                database.Changes.Add(new ChangeSet { RunId = runId, BaseCommitId = failed ? "revision-201" : null,
                    Status = failed ? SourceStatus.Empty : SourceStatus.Unavailable, Patch = failed ? "" : null });
            }
        }
        if (!await database.Runs.AnyAsync(run => run.Id == "run-301", cancellationToken))
        {
            database.Commits.Add(new Commit { Id = "revision-301", Message = "Add nightly smoke job" });
            database.Runs.Add(new Run
            {
                Id = "run-301", PipelineId = "pricing-ci", DatasetVersion = "mock-extra-v1",
                Ref = "refs/heads/nightly-smoke", CommitId = "revision-301",
                StartedAt = new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero),
                State = ExecutionState.Completed, Outcome = Outcome.Failed, Configuration = "Release"
            });
            database.Jobs.Add(new Job { Id = "job-301-smoke-1", RunId = "run-301", Name = "smoke", Attempt = 1,
                State = ExecutionState.Completed, Outcome = Outcome.Failed, LogsStatus = SourceStatus.Truncated, TestsStatus = SourceStatus.Unavailable });
            database.Logs.Add(new LogEntry { JobId = "job-301-smoke-1", Position = 1, Text = "Starting nightly smoke checks. Remaining log output was not retained." });
            database.Changes.Add(new ChangeSet { RunId = "run-301", Status = SourceStatus.Unavailable });
        }
        await database.SaveChangesAsync(cancellationToken);
    }
}
