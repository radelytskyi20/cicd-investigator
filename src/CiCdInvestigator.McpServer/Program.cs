using CiCdInvestigator.Data;
using CiCdInvestigator.McpServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<InvestigatorContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("investigator")
        ?? throw new InvalidOperationException("Missing investigator connection string; start through AppHost.")));
builder.Services.AddHealthChecks().AddCheck<SqlConnectivityCheck>("sql");
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    await InitialDataset.InitializeAsync(scope.ServiceProvider.GetRequiredService<InvestigatorContext>(), app.Lifetime.ApplicationStopping);
}
app.MapHealthChecks("/health");
await app.RunAsync();
