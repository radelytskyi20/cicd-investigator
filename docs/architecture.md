# Architecture

Status: Application shells and Aspire orchestration are implemented and accepted. Initial domain data is implemented and accepted; agent behavior remains planned.

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

AppHost owns SQL Server, API, one MCP process and Vite. SQL publishes external port 14330 through Aspire's proxy, with persistent named volume cicd-investigator-data and a local secret. API receives the MCP endpoint; frontend receives the API endpoint. Domain migrations are implemented in T02-01; MCP protocol tools remain planned.

## Implemented initial data (T02-01)

CiCdInvestigator.Data contains the EF model, initial migration and a compact mock-v1 seed. The MCP host applies migrations and seeds before listening; API does not initialize the database. The Data library is reused by the host and SQL integration test, with API queries deferred to later tasks. There are no generic repositories or additional domain layers.

AppHost maps the investigator resource to investigator-prototype in the existing SQL container. This isolates the approved schema from the withdrawn experiment without deleting prior data. Each job row identifies one run/name/attempt; logs use job identity plus position, and tests refer to that job. Dataset version is obtained through the owning run. Source status belongs to the job's logs/tests or the run's change set; Complete, Empty, Unavailable, NotFound and Truncated are distinct. Not-requested is a future query/session concern, not a stored source state. Optional steps are omitted for this fixture.

Only S01 is seeded. Migration/seed operations use ordinary EF APIs and one SaveChanges call, without custom transaction infrastructure. The seed is intended for a fixed local dataset, not automatic repair of manually edited fixtures. MCP tools, domain query services, reports and SDK sessions remain unimplemented.

## Implemented queries (T02-02)

InvestigationQueries provides ListRunsAsync, GetRunAsync (including jobs), ReadLogAsync, GetTestsAsync, GetChangesAsync and FindBaselineAsync. Queries use AsNoTracking; results wrap detached existing source entities in small response records rather than duplicating all model properties in a mapping layer. QueryStatus separates Ok, NotFound and InvalidInput; source availability/completeness remains SourceStatus within successful responses. SQL failures propagate as execution errors, not empty evidence.

ReadLogAsync defaults to 100 lines and accepts 1-200. It returns actual first/last positions, requested scope and HasMore independently of the source status. An empty requested range does not mean the whole log is empty. Identity is checked against run/job/attempt before reading. Baseline selection returns one nearest eligible predecessor and comparison limitations; it does not claim complete compatibility, compute new diffs or choose a different candidate automatically. No runtime host registers this service yet; transport wiring belongs to T02-03/T04-01.

## Implemented MCP transport (T02-03)

The existing host exposes /mcp using official SDK 2.3.1, with stateless Streamable HTTP and six explicitly registered read-only tools. Each invocation creates a scope for InvestigationQueries and its DbContext. Aspire passes the complete /mcp URL to API. Results are camelCase JSON text with string enums and separate query/source statuses. See mcp-tools.md for the verified catalog. The small console smoke uses the official client and the running SQL-backed host; dedicated T02-04 verification and agent integration remain planned.
