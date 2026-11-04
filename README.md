# AI CI/CD Investigator

A local learning prototype demonstrating evidence-based CI/CD investigation through read-only MCP tools, the Copilot agent loop and active-session memory.

## Current state

Repository preparation in [T01-00](docs/tasks/T01-00.md) is complete and accepted by the owner. T01-01 is accepted; T01-02 application shells and T01-03 orchestration are accepted (Done). Backend/frontend builds and browser API connectivity pass. T01-03 starts SQL/API/MCP/frontend through Aspire; health and browser checks pass. T02-01 and its four subtasks are implemented and accepted (Done): EF migrations, initial S01 data and a real SQL smoke. T02-02 read queries and baseline selection are accepted (Done). T02-03 exposes six read-only MCP tools and is accepted (Done). T02-04 isolated MCP verification is complete and G2 is accepted (Done). T03-01-01 is accepted (Done). T03-01-02 verifies real SDK cancellation, fresh-session isolation and disposal using the existing local login; both subtasks and parent T03-01 are accepted (Done). T03-02 and both approved subtasks implement single-client sequential investigations over SignalR and are accepted (Done); the real SDK/MCP lifecycle smoke passed. T03-03 and both approved subtasks now produce compact evidence-validated reports and are accepted (Done); validator tests and the final real report smoke passed. T03-04, both subtasks and G3 are accepted (Done): the focused follow-up passed, and accepted Stage 3 verification results are consolidated in [Stage 3 review](docs/stage-3-review.md). The owner-approved reduced Stage 4 is implemented and Ready for review: the UI can select the S01 run, investigate through real SDK/MCP, follow up, inspect evidence, Stop, save and reopen independent completed reports. Builds, the real browser path and restart persistence passed; see [Stage 4 review](docs/stage-4-review.md). Stage 5 remains unapproved. Historical observations from the withdrawn Stage 2-3 experiment are retained in [archived research](docs/research/deferred-stage-2-3.md). See docs/development.md for local secrets, Node selection and startup/shutdown.

## Stack and behavior

C#, ASP.NET Core, Aspire, SQL Server in Docker, EF Core, GitHub Copilot SDK for .NET with a compatible Copilot CLI, the official MCP C# SDK, React, TypeScript, Vite and plain CSS. Selected versions and verification limits are recorded in docs/development.md.

Select a completed failed mock run, investigate its sources and baseline, review evidence and hypotheses, and explicitly save a validated completed report as an independent snapshot (revision grouping is deferred). Follow-ups reuse one open SDK session. Closing the tab cancels analysis and discards transient conversation; saved completed reports remain. Persistent memory and automatic repairs are outside scope.

## Repository guide

- [Coding agent instructions](AGENTS.md)
- [Requirements](docs/requirements.md) and [decisions](docs/decisions.md)
- [Task index: 19 tasks and stage gates](docs/tasks/README.md)
- [Git workflow](docs/git-workflow.md)
- [Development and verification](docs/development.md)
- [Architecture](docs/architecture.md)
- [API contracts](docs/api-contracts.md) and [MCP tools](docs/mcp-tools.md)
- [Evaluation](docs/evaluation.md) and [demo](docs/demo.md)
- [Planning sources and provenance](docs/sources.md)

Read AGENTS.md and the assigned task before editing. Work on one task at a time; the owner accepts Done and decides merges. Documentation, UI, generated reports and commits use English; owner discussion may use Russian.
