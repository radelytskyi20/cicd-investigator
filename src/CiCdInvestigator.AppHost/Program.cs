using System.Net.NetworkInformation;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
int sqlPort = builder.Configuration.GetValue("Sql:Port", 14330);
if (sqlPort is < 1024 or > 65535 or 1433)
    throw new InvalidOperationException("Sql:Port must be 1024-65535 and must not be 1433.");
if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == sqlPort))
    throw new InvalidOperationException($"SQL host port {sqlPort} is occupied. Set Sql:Port to another free port (not 1433).");

var password = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password, port: sqlPort)
    .WithImageTag("2022-CU27-ubuntu-22.04")
    .WithDataVolume("cicd-investigator-data");
var database = sql.AddDatabase("investigator");
var mcp = builder.AddProject<Projects.CiCdInvestigator_McpServer>("mcp")
    .WithHttpEndpoint().WithHttpHealthCheck("/health")
    .WithReference(database).WaitFor(sql);
var api = builder.AddProject<Projects.CiCdInvestigator_Api>("api")
    .WithHttpEndpoint().WithHttpHealthCheck("/health")
    .WithReference(database).WithEnvironment("Mcp:Endpoint", mcp.GetEndpoint("http"))
    .WaitFor(sql).WaitFor(mcp);
builder.AddNodeApp("web", "../cicd-investigator-web", "node_modules/vite/bin/vite.js")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WaitFor(api);
await builder.Build().RunAsync();
