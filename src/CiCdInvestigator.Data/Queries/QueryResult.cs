namespace CiCdInvestigator.Data.Queries;

public sealed record QueryResult<T>(QueryStatus Status, T? Data = default, string? Error = null);
