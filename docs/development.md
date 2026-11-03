# Development

## Current baseline

Repository preparation only. No SDK, Node.js or package version has been selected or verified. No solution, application build, test runner, Docker stack or startup command exists yet. This task requires no application SDK installation.

## Repository checks

The following read-only Git commands were executed during T01-00:

```powershell
git status --short
git diff --stat
git remote -v
```

The initial repository had no commits or remote and contained untracked bootstrap documents, C# instructions and local skills. Documentation validation and final staged review results are recorded in [T01-00](tasks/T01-00.md).

## Planned prerequisites and version pins

T01-01 will verify and pin compatible .NET, Aspire, EF Core, MCP SDK, Node.js/npm and Copilot SDK/runtime versions. It will verify Docker with Linux containers and distinguish an installed Copilot executable from working authentication. Do not run environment setup automatically after T01-00.

## Planned commands and checks

T01-02 will document verified restore/build/frontend checks. T01-03 will document AppHost startup, shutdown, restart, endpoint discovery, database readiness and volume retention. Later tasks will add SQL-backed tests, MCP protocol checks and real SDK/browser smoke checks separately from fake adapters. Record actual commands and results when these exist.

## Secrets and runtime state

Use local secrets and sanitized examples; never commit credentials or runtime session files. SDK/CLI persistent-state behavior remains unverified until T03-01. Ignore rules prevent accidental staging; they do not prove runtime cleanup or disable persistent memory.

## Fresh-session recovery check

Planned manual check: open this repository in a fresh Codex session, ask it to read instructions and report the stack, current task, dependency gate, check limitations and next-task restriction without editing files. It must identify T01-00 as awaiting review, require owner acceptance and assignment before T01-01, and distinguish planned application work from implemented repository preparation. This check has not been executed in a fresh session.
