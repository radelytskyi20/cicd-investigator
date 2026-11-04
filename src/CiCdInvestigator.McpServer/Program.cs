using CiCdInvestigator.Data.Queries;
using ModelContextProtocol.AspNetCore;
using CiCdInvestigator.Data;
using CiCdInvestigator.McpServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<InvestigatorContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("investigator")
        ?? throw new InvalidOperationException("Missing investigator connection string; start through AppHost.")));
builder.Services.AddHealthChecks().AddCheck<SqlConnectivityCheck>("sql");
builder.Services.AddScoped<InvestigationQueries>();
builder.Services.AddMcpServer().WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless).WithTools<InvestigationTools>();
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    await InitialDataset.InitializeAsync(scope.ServiceProvider.GetRequiredService<InvestigatorContext>(), app.Lifetime.ApplicationStopping);
}
app.MapHealthChecks("/health");
app.MapMcp("/mcp");
await app.RunAsync();
