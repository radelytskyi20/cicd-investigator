# Implementation decisions

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

The owner approved the five foundation slices linked from the task index. Preserve parent IDs T01-01 through T01-03. Use existing tools without unnecessary installation. SQL Server must publish configurable host port 14330 and must not bind host port 1433, which belongs to the owner's local SQL Server; keep container port 1433. The port mapping is planned, not yet implemented. Runtime and package baseline verification is recorded in development documentation.

## Execution authorization

On November 4, 2026 the owner clarified that execution approval covers only the five foundation slices under T01-01 through T01-03. The earlier interpretation authorizing all Stage 1-5 work is superseded. Stage 2-3 implementation and local branches were withdrawn; Stage 2-5 remain Planned. Before implementing any later stage, break it into small reviewable tasks and obtain explicit owner approval. Preserve focused task branches, verification gates and owner acceptance of Done. See [archived research](research/deferred-stage-2-3.md) for historical observations, not current implementation.
