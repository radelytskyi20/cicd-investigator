namespace CiCdInvestigator.Api;

public sealed record ReportHypothesis(string Explanation, int[] SupportingEvidence, int[] OpposingEvidence);
