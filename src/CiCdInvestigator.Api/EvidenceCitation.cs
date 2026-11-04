namespace CiCdInvestigator.Api;

public sealed record EvidenceCitation(string Tool, string RunId, string? JobId, int? Attempt, string DatasetVersion, string Locator, string Quote, string Completeness, string? SourceLocator = null);
