namespace CiCdInvestigator.Api;

public sealed record InvestigationReport(string RunId, string? BaselineId, string Outcome, string CauseCategory, string Summary, string Confidence, string ConfidenceReason, EvidenceCitation[] Evidence, ReportHypothesis[] Hypotheses, string Limitations, string NextStep);
