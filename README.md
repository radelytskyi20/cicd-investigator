# AI CI/CD Investigator

A planned local learning project demonstrating evidence-based CI/CD investigation through read-only MCP tools, the Copilot agent loop and active-session memory.

## Current state

Repository preparation in [T01-00](docs/tasks/T01-00.md) is complete and accepted by the owner. T01-01 is accepted; T01-02 application shells are Ready for review. Backend/frontend builds and browser API connectivity pass. SQL orchestration and domain functionality are next.

## Planned stack and behavior

C#, ASP.NET Core, Aspire, SQL Server in Docker, EF Core, GitHub Copilot SDK for .NET with a compatible Copilot CLI, the official MCP C# SDK, React, TypeScript, Vite and plain CSS. Selected versions and verification limits are recorded in docs/development.md.

Select a completed failed mock run, investigate its sources and baseline, review evidence and hypotheses, and explicitly save a validated completed report. Follow-ups reuse one open SDK session. Closing the tab cancels analysis and discards transient conversation; saved completed reports remain. Persistent memory and automatic repairs are outside scope.

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
