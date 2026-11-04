using System.Text.Json;
using CiCdInvestigator.Data;
using CiCdInvestigator.Data.Queries;
using Microsoft.EntityFrameworkCore;

namespace CiCdInvestigator.Api;

public static class InvestigationEndpoints
{
    public static void MapInvestigationApi(this WebApplication app)
    {
        app.MapGet("/api/runs", async (InvestigationQueries queries, CancellationToken cancellationToken) =>
            Respond(await queries.ListRunsAsync(cancellationToken: cancellationToken)));
        app.MapGet("/api/runs/{runId}", async (string runId, string? datasetVersion, InvestigationQueries queries, CancellationToken cancellationToken) =>
            await VersionedAsync(runId, datasetVersion, queries, () => queries.GetRunAsync(runId, cancellationToken), cancellationToken));
        app.MapGet("/api/runs/{runId}/baseline", async (string runId, InvestigationQueries queries, CancellationToken cancellationToken) =>
            Respond(await queries.FindBaselineAsync(runId, cancellationToken)));
        app.MapGet("/api/runs/{runId}/changes", async (string runId, string? datasetVersion, InvestigationQueries queries, CancellationToken cancellationToken) =>
            await VersionedAsync(runId, datasetVersion, queries, () => queries.GetChangesAsync(runId, cancellationToken), cancellationToken));
        app.MapGet("/api/runs/{runId}/jobs/{jobId}/logs", async (string runId, string jobId, int attempt, int? start, int? count, string? datasetVersion, InvestigationQueries queries, CancellationToken cancellationToken) =>
            await VersionedAsync(runId, datasetVersion, queries, () => queries.ReadLogAsync(runId, jobId, attempt, start ?? 1, count ?? 100, cancellationToken), cancellationToken));
        app.MapGet("/api/runs/{runId}/jobs/{jobId}/tests", async (string runId, string jobId, int attempt, string? datasetVersion, InvestigationQueries queries, CancellationToken cancellationToken) =>
            await VersionedAsync(runId, datasetVersion, queries, () => queries.GetTestsAsync(runId, jobId, attempt, cancellationToken), cancellationToken));

        app.MapPost("/api/reports", async (SaveReportRequest request, InvestigationAgentService agent, CancellationToken cancellationToken) =>
        {
            var saved = await agent.SaveAsync(request.ResultId, cancellationToken);
            return saved is null
                ? Results.Problem("No matching completed result is available. Wait for the current turn to finish; expired results cannot be saved.", statusCode: 409)
                : Results.Ok(new { saved.Id, saved.SavedAt, saved.RunId });
        });
        app.MapGet("/api/reports", async (InvestigatorContext database, CancellationToken cancellationToken) =>
            Results.Ok(await database.SavedReports.AsNoTracking().OrderByDescending(report => report.SavedAt)
                .Select(report => new { report.Id, report.SavedAt, report.RunId, report.Goal, report.Outcome, report.Summary }).ToListAsync(cancellationToken)));
        app.MapGet("/api/reports/{id:guid}", async (Guid id, InvestigatorContext database, CancellationToken cancellationToken) =>
        {
            var saved = await database.SavedReports.AsNoTracking().SingleOrDefaultAsync(report => report.Id == id, cancellationToken);
            return saved is null ? Results.NotFound() : Results.Ok(new
            {
                saved.Id, saved.SavedAt, saved.RunId, saved.Goal,
                result = JsonSerializer.Deserialize<ReadyReport>(saved.ReportJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            });
        });
    }

    private static IResult Respond<T>(QueryResult<T> result) => result.Status switch
    {
        QueryStatus.Ok => Results.Ok(result),
        QueryStatus.NotFound => Results.NotFound(result),
        _ => Results.BadRequest(result)
    };

    private static async Task<IResult> VersionedAsync<T>(string runId, string? datasetVersion, InvestigationQueries queries,
        Func<Task<QueryResult<T>>> read, CancellationToken cancellationToken)
    {
        if (datasetVersion is not null)
        {
            var run = await queries.GetRunAsync(runId, cancellationToken);
            if (run.Status != QueryStatus.Ok) return Respond(run);
            if (run.Data!.Run.DatasetVersion != datasetVersion)
                return Results.Problem("The original source version is unavailable. The saved quotation is still readable.", statusCode: 410);
        }
        return Respond(await read());
    }
}
