namespace CiCdInvestigator.Data.Queries;

public sealed record LogPage(EvidenceIdentity Identity, SourceStatus SourceStatus, int RequestedStart, int RequestedCount, int? FirstLine, int? LastLine, bool HasMore, IReadOnlyList<LogEntry> Lines);
