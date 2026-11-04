using System.Text.Json;

namespace CiCdInvestigator.Api;

public sealed class InvestigationEvidence
{
    private readonly HashSet<EvidenceCitation> _facts = [];
    private readonly Dictionary<string, JsonElement> _runs = [];
    private readonly Dictionary<EvidenceCitation, string> _locators = [];
    private readonly Dictionary<string, string[]> _jobs = [];
    private readonly HashSet<string> _scope = [];
    private readonly object _sync = new();

    public void Record(string tool, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        lock (_sync)
        {
            _scope.Add(tool);
            if (root.GetProperty("status").GetString() != "Ok") return;
            var data = root.GetProperty("data");
            if (tool == "get_run")
            {
                var run = data.GetProperty("run");
                string runId = Text(run, "id");
                string version = Text(run, "datasetVersion");
                _runs[runId] = run.Clone();
                _jobs[runId] = data.GetProperty("jobs").EnumerateArray().Select(job => Text(job, "name")).Distinct().Order().ToArray();
                Walk(run, "/data/run", new(tool, runId, null, null, version, "", "", "Complete"));
                int index = 0;
                foreach (var job in data.GetProperty("jobs").EnumerateArray())
                {
                    Walk(job, $"/data/jobs/{index++}", new(tool, runId, Text(job, "id"), job.GetProperty("attempt").GetInt32(), version, "", "", "Complete"));
                }
            }
            else if (tool is "read_job_log" or "get_test_results")
            {
                var identity = data.GetProperty("identity");
                Walk(data, "/data", new(tool, Text(identity, "runId"), Text(identity, "jobId"), identity.GetProperty("attempt").GetInt32(), Text(identity, "datasetVersion"), "", "", Text(data, "sourceStatus")));
            }
            else if (tool == "get_changes")
            {
                Walk(data, "/data", new(tool, Text(data, "runId"), null, null, Text(data, "datasetVersion"), "", "", Text(data, "sourceStatus")));
            }
        }
    }

    public string[] Scope() { lock (_sync) return _scope.Order().ToArray(); }

    public string[] Check(InvestigationReport report, InvestigationRequest target)
    {
        lock (_sync)
        {
            if (report.RunId != target.RunId || !_runs.TryGetValue(report.RunId, out var run)
                || Text(run, "state") != "Completed" || Text(run, "outcome") != "Failed")
                throw new ReportValidationException("Read the selected completed failed run before producing a report.");
            if (target.BaselineId is not null && report.BaselineId != target.BaselineId)
                throw new ReportValidationException("The report must use the explicitly selected baseline.");
            var limitations = new List<string>();
            if (report.BaselineId is string baselineId)
            {
                if (!_runs.TryGetValue(baselineId, out var baseline) || Text(baseline, "state") != "Completed"
                    || Text(baseline, "outcome") != "Succeeded" || Text(baseline, "pipelineId") != Text(run, "pipelineId")
                    || Text(baseline, "ref") != Text(run, "ref") || baseline.GetProperty("startedAt").GetDateTimeOffset() >= run.GetProperty("startedAt").GetDateTimeOffset())
                    throw new ReportValidationException("Read an earlier successful baseline of the same pipeline/ref, or use no baseline.");
                foreach (string property in new[] { "environment", "configuration", "datasetVersion" })
                    if (string.IsNullOrWhiteSpace(Text(run, property)) || string.IsNullOrWhiteSpace(Text(baseline, property)) || Text(run, property) != Text(baseline, property))
                        limitations.Add($"Baseline {property} differs or is unknown.");
                if (!_jobs[report.RunId].SequenceEqual(_jobs[baselineId])) limitations.Add("Baseline job sets differ.");
            }
            else limitations.Add("No baseline comparison is included.");
            foreach (var evidence in report.Evidence)
            {
                if (evidence is null || (evidence.RunId != report.RunId && evidence.RunId != report.BaselineId) || !_facts.Contains(evidence with { SourceLocator = null }) || (evidence.SourceLocator is not null && evidence.SourceLocator != _locators[evidence with { SourceLocator = null }]))
                    throw new ReportValidationException($"Evidence {evidence?.Tool} {evidence?.RunId} {evidence?.Locator}: reference, exact quotation, version or attempt does not match an observed MCP result.");
            }
            return limitations.ToArray();
        }
    }

    public EvidenceCitation Resolve(EvidenceCitation citation)
    {
        lock (_sync) return citation with { SourceLocator = _locators[citation with { SourceLocator = null }] };
    }

    private void Walk(JsonElement value, string path, EvidenceCitation identity, string? sourceLocator = null)
    {
        sourceLocator ??= identity.JobId is null ? $"run:{identity.RunId}" : $"job:{identity.JobId}:attempt:{identity.Attempt}";
        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("position", out var position))
            sourceLocator = $"log:{identity.JobId}:attempt:{identity.Attempt}:line:{position.GetInt32()}";
        else if (identity.Tool == "get_test_results" && value.ValueKind == JsonValueKind.Object && value.TryGetProperty("id", out var testId))
            sourceLocator = $"test:{testId.GetString()}:attempt:{identity.Attempt}";
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject()) Walk(property.Value, path + "/" + property.Name.Replace("~", "~0").Replace("/", "~1"), identity, sourceLocator);
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (var item in value.EnumerateArray()) Walk(item, path + "/" + index++, identity, sourceLocator);
        }
        else if (value.ValueKind != JsonValueKind.Null)
        {
            var citation = identity with { Locator = path, Quote = value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText() };
            _facts.Add(citation);
            _locators[citation] = sourceLocator;
        }
    }

    private static string Text(JsonElement value, string property) => value.GetProperty(property).GetString() ?? "";
}
