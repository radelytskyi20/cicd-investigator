namespace CiCdInvestigator.Data.Queries;

public sealed record ChangeEvidence(string DatasetVersion, string RunId, Commit Commit, SourceStatus SourceStatus, string? BaseCommitId, string? Patch);
