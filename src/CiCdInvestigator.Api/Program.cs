if (args.Length == 4 && args[0] == "--copilot-smoke")
{
    await CiCdInvestigator.Api.CopilotSmoke.RunAsync(args[1], args[2], args[3]);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health");
app.MapGet("/api/status", () => new { status = "ready", service = "CI/CD Investigator API" });
await app.RunAsync();
