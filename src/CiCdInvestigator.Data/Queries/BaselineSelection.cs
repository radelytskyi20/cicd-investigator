namespace CiCdInvestigator.Data.Queries;

public sealed record BaselineSelection(Run Target, Run? Candidate, IReadOnlyList<string> TargetJobs, IReadOnlyList<string> CandidateJobs, IReadOnlyList<string> Limitations);
