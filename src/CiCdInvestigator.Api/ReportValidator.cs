using System.Text.Json;

namespace CiCdInvestigator.Api;

public static class ReportValidator
{
    public static InvestigationReport Validate(string json, InvestigationEvidence evidence, InvestigationRequest target)
    {
        InvestigationReport report;
        try
        {
            report = JsonSerializer.Deserialize<InvestigationReport>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
            }) ?? throw new JsonException();
        }
        catch (JsonException) { throw new ReportValidationException("Return a report as valid JSON matching the report contract."); }
        if (report.Outcome is not ("supported" or "unknown") || report.Confidence is not ("low" or "medium" or "high")
            || report.Evidence is null || report.Evidence.Length is < 1 or > 6 || report.Hypotheses is null || report.Hypotheses.Length is < 1 or > 3)
            throw new ReportValidationException("Use a valid outcome, confidence, 1-6 evidence items and 1-3 hypotheses.");
        string[] prose = [report.CauseCategory, report.Summary, report.ConfidenceReason, report.Limitations, report.NextStep];
        if (prose.Any(string.IsNullOrWhiteSpace) || prose.Any(text => text.Length > 900)
            || report.Hypotheses.Any(item => item is null || string.IsNullOrWhiteSpace(item.Explanation) || item.Explanation.Length > 500))
            throw new ReportValidationException("Report explanations must be present and concise.");
        foreach (var hypothesis in report.Hypotheses)
        {
            if (hypothesis.SupportingEvidence is null || hypothesis.OpposingEvidence is null
                || hypothesis.SupportingEvidence.Concat(hypothesis.OpposingEvidence).Any(index => index < 0 || index >= report.Evidence.Length)
                || hypothesis.SupportingEvidence.Intersect(hypothesis.OpposingEvidence).Any())
                throw new ReportValidationException("Hypothesis evidence indexes must refer to distinct evidence items in this report.");
        }
        if (report.Outcome == "supported" && report.Hypotheses.All(item => item.SupportingEvidence.Length == 0))
            throw new ReportValidationException("A supported conclusion needs a hypothesis linked to evidence.");
        int words = string.Join(' ', prose.Concat(report.Hypotheses.Select(item => item.Explanation))).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words > 250 || report.Evidence.Any(item => item is null || item.Quote is null || item.Quote.Length > 1500))
            throw new ReportValidationException("Keep report explanations within 250 words and cite short source values.");
        string[] limits = evidence.Check(report, target);
        return report with { Evidence = report.Evidence.Select(evidence.Resolve).ToArray(), Limitations = string.Join(' ', new[] { report.Limitations }.Concat(limits)) };
    }
}
