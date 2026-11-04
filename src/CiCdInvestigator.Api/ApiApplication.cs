using System.Text.Json.Serialization;
using CiCdInvestigator.Data;
using CiCdInvestigator.Data.Queries;
using Microsoft.EntityFrameworkCore;

namespace CiCdInvestigator.Api;

public static class ApiApplication
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHealthChecks();
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddDbContext<InvestigatorContext>(options => options.UseSqlServer(
            builder.Configuration.GetConnectionString("investigator")
                ?? throw new InvalidOperationException("Missing investigator connection string; start through AppHost.")));
        builder.Services.AddScoped<InvestigationQueries>();
        builder.Services.AddSignalR(options => options.MaximumParallelInvocationsPerClient = 2);
        builder.Services.AddSingleton<InvestigationAgentService>();
        builder.Services.AddHostedService(services => services.GetRequiredService<InvestigationAgentService>());
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            try { await next(context); }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
            catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
            {
                await Results.Problem("Invalid request.", statusCode: exception.StatusCode).ExecuteAsync(context);
            }
            catch (Exception exception) when (!context.Response.HasStarted)
            {
                app.Logger.LogError("API request failed: {ErrorType}", exception.GetType().Name);
                await Results.Problem("The request failed. Please retry.", statusCode: 500).ExecuteAsync(context);
            }
        });
        app.MapHealthChecks("/health");
        app.MapHub<InvestigationHub>("/investigation");
        app.MapGet("/api/status", () => new { status = "ready", service = "CI/CD Investigator API" });
        app.MapInvestigationApi();
        return app;
    }
}
