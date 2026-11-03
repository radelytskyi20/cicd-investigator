var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health");
app.MapGet("/api/status", () => new { status = "ready", service = "CI/CD Investigator API" });
await app.RunAsync();
