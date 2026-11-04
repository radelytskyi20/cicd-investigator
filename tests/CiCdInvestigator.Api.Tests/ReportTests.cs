using System.Text.Json;
using CiCdInvestigator.Api;
using Xunit;

namespace CiCdInvestigator.Api.Tests;

public sealed class ReportTests
{
    private static InvestigationEvidence Sources()
    {
        var source = new InvestigationEvidence();
        source.Record("get_run", """{"status":"Ok","data":{"run":{"id":"r1","datasetVersion":"v1","state":"Completed","outcome":"Failed"},"jobs":[]}}""");
        source.Record("get_test_results", """{"status":"Ok","data":{"identity":{"runId":"r1","jobId":"j1","attempt":1,"datasetVersion":"v1"},"sourceStatus":"Complete","tests":[{"message":"Expected 1.01; actual 1.00"}]}}""");
        return source;
    }

    private static InvestigationReport Report() => new("r1", null, "supported", "rounding", "A rounding mismatch was observed.", "low", "Only a test result is available.",
        [new("get_test_results", "r1", "j1", 1, "v1", "/data/tests/0/message", "Expected 1.01; actual 1.00", "Complete")],
        [new("Rounding behavior may differ.", [0], [])], "No patch inspected.", "Inspect the patch, then rerun the test to verify.");

    [Fact]
    public void ObservedEvidenceCanBeReusedButNotInANewInvestigation()
    {
        var source = Sources();
        string json = JsonSerializer.Serialize(Report());
        Assert.Equal("supported", ReportValidator.Validate(json, source, new("r1", "why")).Outcome);
        Assert.Equal("supported", ReportValidator.Validate(json, source, new("r1", "why")).Outcome);
        Assert.Throws<ReportValidationException>(() => ReportValidator.Validate(json, new(), new("r1", "why")));
    }

    [Theory]
    [InlineData("quote")][InlineData("attempt")][InlineData("job")][InlineData("run")][InlineData("version")][InlineData("locator")][InlineData("completeness")][InlineData("tool")]
    public void AlteredEvidenceIsRejected(string field)
    {
        var report = Report();
        var fact = report.Evidence[0];
        fact = field switch
        {
            "quote" => fact with { Quote = "Test passed" },
            "attempt" => fact with { Attempt = 2 },
            "job" => fact with { JobId = "j2" },
            "run" => fact with { RunId = "r2" },
            "version" => fact with { DatasetVersion = "v2" },
            "locator" => fact with { Locator = "/data/tests/9/message" },
            "completeness" => fact with { Completeness = "Truncated" },
            _ => fact with { Tool = "read_job_log" }
        };
        Assert.Throws<ReportValidationException>(() => ReportValidator.Validate(JsonSerializer.Serialize(report with { Evidence = [fact] }), Sources(), new("r1", "why")));
    }

    [Theory]
    [InlineData("not json")][InlineData("null")][InlineData("{}")][InlineData("{\"outcome\":17}")]
    public void MalformedReportsAreRejected(string json) => Assert.Throws<ReportValidationException>(() => ReportValidator.Validate(json, Sources(), new("r1", "why")));

    [Fact]
    public void UnsupportedTargetBaselineAndHypothesisReferencesAreRejected()
    {
        foreach (var report in new[] { Report() with { RunId = "missing" }, Report() with { BaselineId = "unread" }, Report() with { Hypotheses = [new("Bad link", [5], [])] } })
            Assert.Throws<ReportValidationException>(() => ReportValidator.Validate(JsonSerializer.Serialize(report), Sources(), new("r1", "why")));
    }

    [Fact]
    public void UnknownOutcomeAndUnavailableSourceAreValidWithoutInventingEvidence()
    {
        var source = Sources();
        source.Record("get_test_results", """{"status":"Ok","data":{"identity":{"runId":"r1","jobId":"j1","attempt":1,"datasetVersion":"v1"},"sourceStatus":"Unavailable","tests":[]}}""");
        var report = Report() with { Outcome = "unknown", Evidence = [new("get_test_results", "r1", "j1", 1, "v1", "/data/sourceStatus", "Unavailable", "Unavailable")], Hypotheses = [new("Cause cannot be established.", [], [])] };
        Assert.Equal("unknown", ReportValidator.Validate(JsonSerializer.Serialize(report), source, new("r1", "why")).Outcome);
    }
    [Fact]
    public void LogEvidenceResolvesToOriginalLineInsteadOfPageOffset()
    {
        var source = Sources();
        source.Record("read_job_log", """{"status":"Ok","data":{"identity":{"runId":"r1","jobId":"j1","attempt":1,"datasetVersion":"v1"},"sourceStatus":"Truncated","lines":[{"jobId":"j1","position":42,"text":"Failure at line 42"}]}}""");
        var report = Report() with { Evidence = [new("read_job_log", "r1", "j1", 1, "v1", "/data/lines/0/text", "Failure at line 42", "Truncated")] };
        var result = ReportValidator.Validate(JsonSerializer.Serialize(report), source, new("r1", "why"));
        Assert.Equal("log:j1:attempt:1:line:42", result.Evidence[0].SourceLocator);
    }

    [Fact]
    public void NoncomparableBaselineAddsLimitationsAndIneligibleBaselineIsRejected()
    {
        var source = Sources();
        source.Record("get_run", """{"status":"Ok","data":{"run":{"id":"r1","datasetVersion":"v1","state":"Completed","outcome":"Failed","pipelineId":"p1","ref":"main","startedAt":"2026-10-20T10:00:00Z","environment":"sdk-a","configuration":"Release"},"jobs":[]}}""");
        source.Record("get_run", """{"status":"Ok","data":{"run":{"id":"r0","datasetVersion":"v1","state":"Completed","outcome":"Succeeded","pipelineId":"p1","ref":"main","startedAt":"2026-10-19T10:00:00Z","environment":"sdk-b","configuration":"Release"},"jobs":[]}}""");
        string json = JsonSerializer.Serialize(Report() with { BaselineId = "r0" });
        Assert.Contains("environment differs", ReportValidator.Validate(json, source, new("r1", "why")).Limitations);
        Assert.Throws<ReportValidationException>(() => ReportValidator.Validate(json, source, new("r1", "why", "other")));
        source.Record("get_run", """{"status":"Ok","data":{"run":{"id":"r0","datasetVersion":"v1","state":"Completed","outcome":"Succeeded","pipelineId":"other","ref":"main","startedAt":"2026-10-19T10:00:00Z"},"jobs":[]}}""");
        Assert.Throws<ReportValidationException>(() => ReportValidator.Validate(json, source, new("r1", "why")));
    }

    [Fact]
    public void OverlongReportAndSuccessfulTargetAreRejected()
    {
        var source = Sources();
        var report = Report() with { Summary = string.Join(' ', Enumerable.Repeat("word", 251)) };
        Assert.Throws<ReportValidationException>(() => ReportValidator.Validate(JsonSerializer.Serialize(report), source, new("r1", "why")));
        source.Record("get_run", """{"status":"Ok","data":{"run":{"id":"r1","datasetVersion":"v1","state":"Completed","outcome":"Succeeded"},"jobs":[]}}""");
        Assert.Throws<ReportValidationException>(() => ReportValidator.Validate(JsonSerializer.Serialize(Report()), source, new("r1", "why")));
    }
}
