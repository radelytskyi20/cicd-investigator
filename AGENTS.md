# Coding agent instructions

## Purpose and stack
Build a small local AI CI/CD Investigator demonstrating tools, the agent loop and session memory. Use C#, ASP.NET Core, Aspire, SQL Server in Docker, EF Core, GitHub Copilot SDK for .NET, official MCP C# SDK, React, TypeScript and Vite. Use pinned repository versions; do not replace the stack silently.

## Context
Before work, read [README.md](README.md), [decisions](docs/decisions.md), [requirements](docs/requirements.md), the [task index](docs/tasks/README.md), the assigned task and relevant code/documents. Read applicable AGENTS.md files. [Architecture](docs/architecture.md) distinguishes planned from implemented structure. Report contradictions rather than inventing accepted decisions.

## Architecture and scope
Aspire starts SQL, API, MCP and frontend. The agent reads CI/CD facts through read-only MCP; API saves completed reports. Add shared libraries only for real shared logic. Avoid extra layers, generic repositories and a second LLM loop. Runtime Investigator prompts and evaluator answers are separate from developer instructions.

## Session lifecycle
Reuse messages and tool results only inside the active SDK session. Closing the tab cancels analysis and discards transient state. Keep explicitly saved completed reports. Do not add persistent memory, resume, chat storage or background jobs.

## Task workflow
Check Git status and completed dependencies first. Implement only the assigned task, preserve unrelated user changes, run relevant checks and update affected documentation. Set Ready for review and return a concise review report. Do not start another task automatically. The owner accepts Done and any merge.

## Checks
Use verified commands from [development](docs/development.md). Build changed components and run relevant tests when applications exist; T01-00 needs documentation and Git checks only. Check MCP through a protocol client. Real SDK/AI smoke is separate from fake adapter tests. Do not claim unexecuted checks passed or add tests that only mirror class/text existence.

## Documentation
All repository documentation and agent-facing text must be English, including task descriptions, outcomes and Git workflow. Exact source quotations retain their content. Keep this file concise; detailed behavior belongs in docs. Update code and affected docs together. Label planned work, proposals and verified implementation accurately. Do not invent versions, endpoints or commands.

## Git
Follow [Git workflow](docs/git-workflow.md). Use one task/Txx-xx branch and focused English commits. Inspect staged diff and include only task-related changes. Preserve unrelated work. No automatic push, merge, force-push or history rewriting. Never commit secrets, build output or runtime session files. Report missing Git identity rather than claiming a commit exists.

## Code and local skills
For C# changes, follow [.github/instructions/coding-style.instructions.md](.github/instructions/coding-style.instructions.md); its legacy examples do not add legacy projects to this stack. Existing local skills and skills-lock.json were present before T01-00. Use skills only when relevant or explicitly invoked; their presence does not assign work.

## Completion report
Report implemented changes, actual checks and results, remaining limitations, updated docs and commit hash. Explicitly identify unverified integration. Record repository task outcomes in English; discussion with the owner can follow the owner's language.
