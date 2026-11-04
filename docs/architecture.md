# Architecture

Status: Application shells and Aspire orchestration are implemented and accepted. Domain data and agent behavior remain planned.

## Planned components

Aspire AppHost will orchestrate SQL Server, the ASP.NET Core API, one separate MCP host and the React/Vite frontend. A Data library will be added when EF Core logic is actually shared. See [decisions](decisions.md) for proposals and ownership.

## Planned data flow

The UI reads catalog and source data through the API. The Investigator reads CI/CD facts through read-only MCP tools. Copilot CLI owns the agent loop; the application owns domain instructions, tool access, evidence validation and connection lifecycle. The API saves explicitly requested, validated completed reports.

## Planned lifecycle and boundaries

One open connection owns active-session conversation and tool results. Disconnect cancels work and disposes transient state. Saved reports do not restore chat. Developer instructions, runtime Investigator prompts and evaluator expectations must remain separate; evaluator answers must never be exposed through MCP or the runtime working directory.

## Future implementation record

T01-02 and T01-03 will record actual structure and orchestration. Stages 2–4 will record schema, transport and lifecycle behavior. No endpoint, migration owner or runtime-storage behavior is verified yet.

## Implemented shells

The solution contains src/CiCdInvestigator.AppHost, src/CiCdInvestigator.Api and src/CiCdInvestigator.McpServer. The separately built React/TypeScript/Vite frontend is src/cicd-investigator-web. Both servers expose /health; API exposes /api/status. No unused ServiceDefaults or Shared project was introduced.

## Implemented orchestration

AppHost owns SQL Server, API, one MCP process and Vite. SQL publishes external port 14330 through Aspire's proxy, with persistent named volume cicd-investigator-data and a local secret. API receives the MCP endpoint; frontend receives the API endpoint. Domain migrations and MCP protocol tools are still planned.
