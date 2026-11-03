var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.CiCdInvestigator_Api>("api").WithHttpEndpoint();
builder.AddProject<Projects.CiCdInvestigator_McpServer>("mcp").WithHttpEndpoint();
await builder.Build().RunAsync();
