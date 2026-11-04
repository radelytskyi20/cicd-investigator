# Implementation decisions

## Approved small dataset expansion (November 4, 2026)

The owner authorized a 15-minute addition of a few simple investigable runs. T05-01 is limited to adding S02 (restore failure with a successful predecessor) and S04 (insufficient data), preserving S01 and saved reports. Use the existing schema and agent behavior. Verify seed/query behavior and one real investigation per new failure; this does not authorize or complete the full S01–S09 catalog or T05-02 repeated evaluation.

## Approved Stage 4 demo reduction (November 4, 2026)

The owner authorized the reduced Stage 4 as one sequential delivery for a working local demo by 15:30 Europe/Budapest. This supersedes the earlier requirement for separate Stage 4 execution approvals and the broader original task acceptance scope.

Use the existing S01 failed run and successful baseline. Implement a plain run list, source details, real SDK/MCP investigation, actual tool activity, compact report, same-session follow-up, Stop, explicit Save and a basic saved-report list/open action. Keep the single-client model. No new scenario catalog is required for this delivery.

Each explicitly saved completed result is an independent immutable report. Revision grouping and significant-change detection are deferred. Save uses a server-owned ready-result ID and is idempotent for that ID. Only the latest validated result is saveable while its session is open and idle; a successful follow-up replaces it. Stop or validation failure can retain the previous result if the session remains alive. A fresh Start or session disposal clears it. No auto-save or persistent chat is introduced.

Catalog filters, manual baseline UI, full chat transcript UI, rich source viewers and historical source reconstruction are deferred. Version mismatches must still never silently substitute a different source. Retain basic errors without building an extensive failure matrix.

Verify changed builds, existing report validation tests, official MCP client connectivity and a real browser happy path (S01, follow-up, evidence, Save, reopen). Check Stop, tab closure and saved history after restart once. Do not add a fake SDK or broad browser suite merely for this stage. Stage 5 full catalog and repeated evaluation remain unapproved. Owner acceptance of Done/G4 and merges remains separate.

## Accepted baseline

The project is a local AI CI/CD Investigator with React, TypeScript and Vite SPA; ASP.NET Core Web API; separate .NET MCP server; SQL Server in Docker; EF Core; and Aspire as the startup orchestrator. GitHub.Copilot.SDK for .NET controls a compatible Copilot CLI runtime.

The learning assignment requires tools, the agent loop and memory. Keep short-term conversation and tool results in one active SDK session. This later decision supersedes the original PRD v0.3 exclusion of all memory. No cross-session memory, resume or background analysis is required. Tab closure ends active work; explicitly saved completed reports and revisions remain.

All project documentation must be English: AGENTS.md, README, requirements, decisions, architecture, development commands, API/MCP references, task descriptions, task outcomes, Git rules and demo/evaluation notes. Commit messages are English. Project discussion with the owner may remain Russian. UI and generated investigation explanations/reports use English. Exact source identifiers and log quotations keep their original content.

T01-00 comes first and prepares instructions, documentation and Git workflow. T01-01 follows owner acceptance and configures the environment. Existing task IDs are preserved; there are 19 tasks across five stages. This package describes planned work and does not claim any task has been implemented.

## Technical proposals for review

Use Streamable HTTP for MCP so Aspire owns one server process. Use SignalR for conversation, events and connection-scoped cancellation with automatic reconnect disabled. Add one CiCdInvestigator.Data library only when actual shared EF logic appears. Let the MCP host apply migrations and seed before readiness; API does not race migrations.

The user catalog may read shared query services through REST. The investigator obtains CI/CD facts through MCP. API validates and saves completed reports. CLI owns the tool-use loop; application code owns domain instructions, tool access, validation and lifecycle. Do not build a second LLM loop or a universal provider layer.

Pin actual versions in T01-01 and validate SDK/runtime behavior in T03-01. Source log range limits are data-reading contracts, not the postponed agent-work budgets. Verify SDK/CLI service files separately from application memory: do not promise zero persistent runtime state without checking supported settings and cleanup.

Disconnect cancellation occurs after transport detects the closure; measure actual latency. Never rely only on beforeunload, never resume automatically, and clean up only application-owned sessions and files.

## Scope

Keep run catalog, investigation, baseline comparison, hypotheses/evidence, open-session follow-up and completed-report history. No real CI integration, automatic repair, shell/write tools for the investigator, persistent chat, import/export, background queue, elaborate UI or accessibility project. S01-S09 remain the dataset basis. One negative demonstration example is sufficient; evaluate the primary and negative cases at least three times each.

## Remaining small choices

S01 as primary and S04 as the negative demonstration are proposed defaults, not earlier explicit owner choices. One mock repository is proposed; use only enough pipelines/configurations for meaningful examples. SDK version, runtime source and exact transport APIs are chosen from actual compatibility checks. These choices do not authorize expanding product scope.

## Accepted foundation setup (November 4, 2026)

The owner approved the five foundation slices linked from the task index. Preserve parent IDs T01-01 through T01-03. Use existing tools without unnecessary installation. SQL Server must publish configurable host port 14330 and must not bind host port 1433, which belongs to the owner's local SQL Server; keep container port 1433. The port mapping is implemented and accepted with Foundation. Runtime and package baseline verification is recorded in development documentation.

## Execution authorization

On November 4, 2026 the owner clarified that execution approval covers only the five foundation slices under T01-01 through T01-03. The earlier interpretation authorizing all Stage 1-5 work is superseded. Stage 2-3 implementation and local branches were withdrawn; Stage 2-5 remain Planned. Before implementing any later stage, break it into small reviewable tasks and obtain explicit owner approval. Preserve focused task branches, verification gates and owner acceptance of Done. See [archived research](research/deferred-stage-2-3.md) for historical observations, not current implementation.

## Owner acceptance and prototype scope (November 4, 2026)

The owner accepted all Foundation tasks previously Ready for review, completing G1, and approved the four T02-01 subtasks in the task index. This supersedes foundation-only authorization for T02-01 only; subsequent tasks still need approval.

Build a minimal one-time local working prototype, not a deployed production service. Prefer simple EF operations. Do not invest in custom transaction orchestration, extensive durability testing, connection stress testing or broad protocol matrices. Keep relevant builds and minimal functional checks: a real SQL-backed data smoke and, when MCP tools exist, a minimal protocol-client smoke. Real SDK verification remains separate from fake adapters. Evidence identities and session boundaries remain required.

## T02-01 implementation choices

MCP owns migration and seed before readiness. The investigator Aspire resource maps to investigator-prototype, isolating the withdrawn experiment's database without deleting it. The Data library holds actual EF logic reused by MCP and the SQL smoke; API consumption is deferred. Optional steps are omitted. The prototype uses ordinary EF SaveChanges and a fixed idempotent seed, without custom transaction or recovery infrastructure.

## T02-02 approved prototype scope (November 4, 2026)

The owner approved the four T02-02 slices and implementation. Simplicity is required wherever the demonstration allows it. Use a single concrete EF query service and small result records; no new architecture layers. Read log ranges only (1-200 lines). Select the nearest eligible baseline by strictly earlier StartedAt, breaking timestamp ties by ID. Compare available environment/SDK text without a parser, configuration and job names; unknown values remain limitations. Keep a noncomparable candidate visible instead of implementing fallback scoring. Stored changes retain their own base revision. T02-03 and later work still require approval.

## T02-03 approval and implementation

On November 4, 2026 the owner accepted T02-02 and all four subtasks and authorized T02-03-01 and T02-03-02. The minimal catalog contains six tools: jobs are included in get_run and find_baseline returns a single qualified candidate. Use stateless Streamable HTTP with one Aspire-owned process, per-call EF scope and JSON text results with explicit domain statuses. No AI integration or T02-04 acceptance is implied.

## T02-04 execution approval

On November 4, 2026 the owner approved T02-04-01. Reuse the existing real MCP client and host; add one focused xUnit integration test on a dedicated database for truncated logs and distinct attempts. This approval does not start Stage 3. G2 is submitted for owner review after successful checks.

## T03-01 local prototype scope (November 4, 2026)

The owner approved T03-01-01 and T03-01-02 and their execution. Reuse the installed CLI and existing local login; create fresh SDK conversations without implementing credential management. CLI-managed session files are acceptable and are not a blocker. This supersedes the earlier requirement to disable, isolate or clean runtime storage. Do not change global settings or remove unrelated sessions. No custom storage isolation or transcript cleanup is required. Application conversation context remains active-session-only, with no automatic resume or cross-session memory; cancellation and ordinary SDK disposal still require verification. T03-02 and later tasks remain separately gated.

## T03-01 lifecycle outcome

T03-01-01 was accepted by the owner on November 4, 2026. T03-01-02 verified real SDK cancellation, fresh-session isolation and normal disposal using the same installed CLI and login. Reuse CreateSessionAsync rather than ResumeSessionAsync. Retain the simple CLI-managed storage behavior approved above. The owner accepted T03-01-02 on November 4, 2026, completing both subtasks and parent T03-01 (Done). This does not accept G3 or authorize T03-02.

## Approved single-client T03-02 scope (November 4, 2026)

The owner approved T03-02-01 and T03-02-02 for implementation. This local prototype has one client and one current investigation application-wide. No multi-client session isolation or parallel investigations are required. After an answer or cancellation finishes, an explicit follow-up continues the conversation or Start replaces it with a fresh investigation. No separate close-investigation command is needed. Disconnect and shutdown still cancel work and release the current SDK session; ordinary CLI-managed files remain acceptable. This supersedes the earlier per-tab multi-session wording. T03-03 and later tasks remain unapproved.

## T03-02 owner acceptance (November 4, 2026)

The owner accepted T03-02-01 and T03-02-02, completing parent T03-02 (Done). T03-03 and T03-04 remain Planned and require separate scope approval. G3 is not yet accepted.

## Approved compact-report scope (November 4, 2026)

The owner approved T03-03-01 and T03-03-02. Reports must remain a few short paragraphs, comparable to the reviewed example: conclusion, facts/evidence, a relevant alternative, confidence with reason, limits and a useful next check. Every follow-up returns an updated complete compact report. Use a minimal insufficient-data fixture only in a test database; do not expand the production S01 dataset or implement the full Stage 5 catalog. Keep one client, one active investigation, the existing login and SDK-owned agent loop. T03-04 remains separately gated.

## T03-03 owner acceptance (November 4, 2026)

The owner accepted T03-03-01 and T03-03-02, completing parent T03-03 (Done). T03-04 remains Planned and is the remaining Stage 3 task. G3 is not yet accepted; its verification scope requires separate approval.

## Approved Stage 3 closeout scope (November 4, 2026)

The owner approved T03-04-01 and T03-04-02: add one focused substantive follow-up check and consolidate G3 evidence. Reuse accepted T03-01 through T03-03 results rather than rerun the full suite or introduce a fake SDK layer merely to close the stage. This supersedes the earlier broader deterministic-lifecycle testing plan in T03-04. Rerun checks only when a relevant change or unresolved concern justifies it. G3 acceptance remains the owner's decision; Stage 4 is not authorized.

## Stage 3 submission (November 4, 2026)

The approved T03-04 slices are complete and Ready for review. A focused real follow-up exposed an uninspected-versus-unavailable wording error; a domain-instruction clarification and focused rerun resolved that observed issue. Accepted earlier tests were reused, with no new fake SDK layer or full-suite rerun. G3 is Ready for review, not Done. The evidence map and prototype boundaries are in stage-3-review.md; Stage 4 remains unstarted and unapproved.

## Stage 3 owner acceptance (November 4, 2026)

The owner accepted T03-04-01 and T03-04-02, completing parent T03-04 and G3 (Done). No Stage 3 implementation tasks remain. Recorded verification limits remain unchanged. Stage 4 requires a separately reviewed breakdown and execution approval.
