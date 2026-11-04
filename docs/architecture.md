# Architecture

Status: Foundation, domain data/MCP, SDK sessions and compact report generation are implemented and accepted through T03-03. T03-04/G3 is Ready for review; see stage-3-review.md. UI and report persistence remain planned.

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

## Implemented SDK experiment (T03-01-01)

The API executable has an explicit --copilot-smoke mode, invoked by scripts/Test-Copilot.ps1 against the existing Aspire MCP host. Ordinary API startup remains unchanged. GitHub.Copilot.SDK 1.0.28 uses the installed CLI 1.0.105 and existing login. A fresh session exposes only get_run, records tool events/source payload availability, and verifies a final answer and idle event. No agent service, hub or report persistence is implemented. Runtime files use ordinary CLI behavior; custom storage cleanup is out of scope.

## Verified SDK lifecycle experiment (T03-01-02)

The API's explicit --copilot-lifecycle-smoke mode verifies same-session recall, abort after a real tool-start event, idle and a three-second no-further-work observation, then a distinct fresh SDK session without the previous marker. Sessions and client use ordinary await-using disposal. Installed CLI child-process exit was observed separately. This is a local SDK integration check, not a connection-scoped agent service; SignalR and browser disconnect behavior remain T03-02.

## Implemented single-client agent service (T03-02)

ApiApplication builds the same API used by normal startup and the real SignalR integration smoke. InvestigationHub forwards Start/FollowUp/Stop to one InvestigationAgentService singleton. The service holds one SDK conversation and serializes turns with a single gate; no per-client dictionary, provider layer or second model loop exists. A new Start after completion creates fresh context; FollowUp retains the current SDK context. Six existing read-only MCP tools are explicitly allowed. A turn-local channel forwards actual SDK events to SignalR and is drained or cancelled within the awaited request; it is not a background investigation queue.

Stop cancels the current turn while retaining the conversation. Disconnect and application shutdown cancel and dispose it. The one-client scope supersedes the earlier planned per-tab session wording. UI, validated reports and saved history remain later tasks.

## Implemented report generation and validation (T03-03)

InvestigatorInstructions supplies domain and compact-output guidance through the SDK system-message configuration. The existing SDK/CLI chooses and executes tool calls; no extra model loop or provider layer was introduced. Native ToolExecutionCompleteEvent result content exposes the MCP JSON, so no tool-to-MCP bridge is needed.

InvestigationEvidence normalizes observed scalar source values with dataset/run/job/attempt identities and locators for validation. This is transient application state used only to check the current investigation, not persistent memory or a source archive. ReportValidator checks the generated report, then emits answerReady with duration and tool count. Validation failures retain the conversation for an explicit retry. One small insufficient-data fixture exists only in investigator-report-test; the ordinary mock dataset remains S01.
