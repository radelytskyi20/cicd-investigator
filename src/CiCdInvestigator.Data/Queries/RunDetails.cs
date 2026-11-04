namespace CiCdInvestigator.Data.Queries;

public sealed record RunDetails(Run Run, IReadOnlyList<Job> Jobs);
