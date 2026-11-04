using Microsoft.EntityFrameworkCore;

namespace CiCdInvestigator.Data.Queries;

public sealed class InvestigationQueries(InvestigatorContext database)
{
    public const int MaxLogLines = 200;

    public async Task<QueryResult<IReadOnlyList<Run>>> ListRunsAsync(
        string? pipeline = null, string? reference = null, Outcome? outcome = null, CancellationToken cancellationToken = default)
    {
        if (pipeline is not null && string.IsNullOrWhiteSpace(pipeline)
            || reference is not null && string.IsNullOrWhiteSpace(reference)
            || outcome is not null && !Enum.IsDefined(outcome.Value))
            return new(QueryStatus.InvalidInput, Error: "Filters must be nonempty and outcome must be valid.");
        var query = database.Runs.AsNoTracking();
        if (pipeline is not null) query = query.Where(x => x.PipelineId == pipeline);
        if (reference is not null) query = query.Where(x => x.Ref == reference);
        if (outcome is not null) query = query.Where(x => x.Outcome == outcome);
        return new(QueryStatus.Ok, await query.OrderByDescending(x => x.StartedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken));
    }

    public async Task<QueryResult<RunDetails>> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) return new(QueryStatus.InvalidInput, Error: "Run identity is required.");
        var run = await database.Runs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == runId, cancellationToken);
        if (run is null) return new(QueryStatus.NotFound, Error: "Run not found.");
        var jobs = await database.Jobs.AsNoTracking().Where(x => x.RunId == runId)
            .OrderBy(x => x.Name).ThenBy(x => x.Attempt).ToListAsync(cancellationToken);
        return new(QueryStatus.Ok, new(run, jobs));
    }

    public async Task<QueryResult<LogPage>> ReadLogAsync(string runId, string jobId, int attempt,
        int start = 1, int count = 100, CancellationToken cancellationToken = default)
    {
        if (start < 1 || count < 1 || count > MaxLogLines)
            return new(QueryStatus.InvalidInput, Error: $"Start must be positive and count must be 1-{MaxLogLines}.");
        var source = await FindJobAsync(runId, jobId, attempt, cancellationToken);
        if (source.Status != QueryStatus.Ok) return new(source.Status, Error: source.Error);
        var (identity, job) = source.Data;
        var lines = await database.Logs.AsNoTracking().Where(x => x.JobId == jobId && x.Position >= start)
            .OrderBy(x => x.Position).Take(count + 1).ToListAsync(cancellationToken);
        bool hasMore = lines.Count > count;
        if (hasMore) lines.RemoveAt(lines.Count - 1);
        return new(QueryStatus.Ok, new(identity, job.LogsStatus, start, count,
            lines.Count == 0 ? null : lines[0].Position, lines.Count == 0 ? null : lines[^1].Position, hasMore, lines));
    }

    public async Task<QueryResult<TestEvidence>> GetTestsAsync(string runId, string jobId, int attempt,
        CancellationToken cancellationToken = default)
    {
        var source = await FindJobAsync(runId, jobId, attempt, cancellationToken);
        if (source.Status != QueryStatus.Ok) return new(source.Status, Error: source.Error);
        var (identity, job) = source.Data;
        var tests = await database.Tests.AsNoTracking().Where(x => x.JobId == jobId).OrderBy(x => x.Id).ToListAsync(cancellationToken);
        return new(QueryStatus.Ok, new(identity, job.TestsStatus, tests));
    }

    public async Task<QueryResult<ChangeEvidence>> GetChangesAsync(string runId, CancellationToken cancellationToken = default)
    {
        var result = await GetRunAsync(runId, cancellationToken);
        if (result.Status != QueryStatus.Ok) return new(result.Status, Error: result.Error);
        var run = result.Data!.Run;
        var commit = await database.Commits.AsNoTracking().SingleAsync(x => x.Id == run.CommitId, cancellationToken);
        var changes = await database.Changes.AsNoTracking().SingleOrDefaultAsync(x => x.RunId == runId, cancellationToken);
        return new(QueryStatus.Ok, new(run.DatasetVersion, run.Id, commit,
            changes?.Status ?? SourceStatus.Unavailable, changes?.BaseCommitId, changes?.Patch));
    }

    public async Task<QueryResult<BaselineSelection>> FindBaselineAsync(string runId, CancellationToken cancellationToken = default)
    {
        var result = await GetRunAsync(runId, cancellationToken);
        if (result.Status != QueryStatus.Ok) return new(result.Status, Error: result.Error);
        var target = result.Data!.Run;
        var candidate = await database.Runs.AsNoTracking().Where(x => x.PipelineId == target.PipelineId
            && x.Ref == target.Ref && x.StartedAt < target.StartedAt
            && x.State == ExecutionState.Completed && x.Outcome == Outcome.Succeeded)
            .OrderByDescending(x => x.StartedAt).ThenBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        var targetJobs = result.Data.Jobs.Select(x => x.Name).Distinct().Order().ToArray();
        if (candidate is null) return new(QueryStatus.Ok, new(target, null, targetJobs, [], ["No preceding successful run is available."]));
        var candidateJobs = await database.Jobs.AsNoTracking().Where(x => x.RunId == candidate.Id)
            .Select(x => x.Name).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken);
        var limits = new List<string>();
        Compare("Environment/SDK", target.Environment, candidate.Environment, limits);
        Compare("Configuration", target.Configuration, candidate.Configuration, limits);
        if (!targetJobs.SequenceEqual(candidateJobs)) limits.Add("Job sets differ.");
        if (target.DatasetVersion != candidate.DatasetVersion) limits.Add("Dataset versions differ.");
        return new(QueryStatus.Ok, new(target, candidate, targetJobs, candidateJobs, limits));
    }

    private async Task<QueryResult<(EvidenceIdentity Identity, Job Job)>> FindJobAsync(
        string runId, string jobId, int attempt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(jobId) || attempt < 1)
            return new(QueryStatus.InvalidInput, Error: "Run, job and positive attempt identities are required.");
        var job = await database.Jobs.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == jobId && x.RunId == runId && x.Attempt == attempt, cancellationToken);
        if (job is null) return new(QueryStatus.NotFound, Error: "Job attempt not found for the selected run.");
        string version = await database.Runs.Where(x => x.Id == runId).Select(x => x.DatasetVersion).SingleAsync(cancellationToken);
        return new(QueryStatus.Ok, (new(version, runId, jobId, attempt), job));
    }

    private static void Compare(string label, string? target, string? candidate, List<string> limitations)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(candidate)) limitations.Add($"{label} is unknown for at least one run.");
        else if (target != candidate) limitations.Add($"{label} differs.");
    }
}
