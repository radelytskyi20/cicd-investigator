namespace CiCdInvestigator.Api;

public sealed record ReadyReport(InvestigationReport Report, long DurationMs, int ToolCallCount, string[] InspectedSources);
