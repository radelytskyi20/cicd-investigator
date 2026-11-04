namespace CiCdInvestigator.Api;

public static class ApiApplication
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHealthChecks();
        builder.Services.AddSignalR(options => options.MaximumParallelInvocationsPerClient = 2);
        builder.Services.AddSingleton<InvestigationAgentService>();
        builder.Services.AddHostedService(services => services.GetRequiredService<InvestigationAgentService>());
        var app = builder.Build();
        app.MapHealthChecks("/health");
        app.MapHub<InvestigationHub>("/investigation");
        app.MapGet("/api/status", () => new { status = "ready", service = "CI/CD Investigator API" });
        return app;
    }
}
