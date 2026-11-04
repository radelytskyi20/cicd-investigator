namespace CiCdInvestigator.Api;

public sealed record InvestigationEvent(string Type, string TurnId, string? Text = null, string? ToolName = null, string? ToolCallId = null);
