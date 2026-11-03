using CiCdInvestigator.McpServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ConnectivityContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("investigator")
        ?? throw new InvalidOperationException("Missing investigator connection string; start through AppHost.")));
builder.Services.AddHealthChecks().AddCheck<SqlConnectivityCheck>("sql");
var app = builder.Build();
app.MapHealthChecks("/health");
await app.RunAsync();
