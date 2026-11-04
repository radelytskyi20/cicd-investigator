namespace CiCdInvestigator.Api;

public sealed record InvestigationRequest(string RunId, string Goal, string? BaselineId = null);
