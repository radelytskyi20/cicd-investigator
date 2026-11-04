# Deferred Stage 2-3 research (archived November 4, 2026)

## Scope and status

The owner clarified that only five foundation slices under T01-01 through T01-03 were authorized. Stage 2-3 implementation and its local branch history were removed from the active baseline. This note preserves observations, not implementation, acceptance or permission to resume. Break each later stage into small tasks and obtain owner approval before implementation. Stage 2-5 tasks are Planned again; the earlier T03-01 acceptance applied to the now-withdrawn experiment.

Retained foundation: ac404ef, preceded by 724ac5c and dc0bee8. Withdrawn work: dfc2d54 (data), 836e548 (queries), a86de7d (MCP), 113eabb (protocol checks), ceef56c (SDK smoke), 05fcaeb (smoke acceptance). Hashes are provenance only, not dependencies. No remote push or merge occurred. Normal Git reflogs may temporarily retain unreachable commits; no repository-wide garbage collection is required.

## Data and queries explored

- One shared Data library held actual EF logic. MCP alone applied migrations/seed before readiness; API did not race migrations.
- Model: Pipeline, Run, Job with attempt, LogEntry keyed by job/line, TestResult, Commit, ChangeSet. Foreign keys used Restrict. Run state and outcome remained separate.
- Synthetic S01: pipeline-main, sample/calculator, run-101 successful baseline and run-102 failed, commits a100/a101, main, SDK 10.0.401, Release. A rounding test expected 12.34 but got 12.35; available diff changed ToEven to AwayFromZero. Restore warning was a distractor, not the failure. Evaluator answers must remain separate from agent sources.
- SourceFact carried locator, dataset version, run/job/attempt, kind, exact text and completeness. Locators included run:, job:, log:job:line, test:, commit:, diff:.
- Queries distinguished unavailable, empty, not_found, invalid_input and partial results. Logs were bounded to 200 lines per request; list_runs to 100. Baseline candidates were preceding completed successes on the same pipeline/ref; SDK/config/job-set/version mismatches weakened comparability.
- Do not blindly reuse the old seed's simple run-102 existence marker: partial seeds and dataset upgrades need coherent idempotence and preserved evidence versions.

## Real MCP and SQL observations

- Official ModelContextProtocol.AspNetCore 2.3.1 supported Streamable HTTP at /mcp, AddMcpServer().WithHttpTransport().WithTools<T>() and McpServerTool read-only annotations.
- Seven tested tools: list_runs, get_run, get_jobs, read_job_log, get_test_results, get_changes, find_baselines. No SQL executor or write tools.
- Official McpClient with HttpClientTransport discovered and called tools against actual Kestrel and SQL. JSON property names were camelCase; null fields could be omitted rather than present as null.
- Three SQL-backed xUnit tests passed: migration/seed/relationships; query edge cases; actual HTTP MCP discovery/calls. Each fixture created and removed only its own investigator_test_<guid> database. These tests are removed with deferred code, not current foundation tests.
- Wait for SQL healthy before tests. Use Release checks when Aspire holds Debug binaries open on Windows. EF InMemory does not replace SQL/protocol checks.
- Exploratory package versions: dotnet-ef and EF Design 10.0.14, Microsoft.NET.Test.Sdk 18.11.0, xunit 2.9.3, runner.visualstudio 4.0.1. Revalidate when those tasks are approved.

## Real Copilot SDK observations

- Pinned GitHub.Copilot.SDK 1.0.28 metadata selected CLI 1.0.105, matching the installed CLI. Disable automatic CLI download and use the installed executable through RuntimeConnection.ForStdio. gpt-5.4 worked in the experiment; availability may change.
- SDK owns the runtime and tool-use loop. No Process.Start replacement, handwritten JSON-RPC, second model loop or alternate model provider is needed.
- Empty mode unconditionally sets COPILOT_DISABLE_KEYTAR=1, preventing use of the existing OS credential store. Account metadata alone did not solve auth in that mode.
- For this single-user local experiment, CopilotCli mode worked with isolated BaseDirectory/WorkingDirectory and explicit restrictions. It is not a multi-tenant design. Authentication reuse was explicitly authorized by the owner for synthetic project data.
- The temporary config copied only host/login/kind from loggedInUsers and host/login from lastLoggedInUser. Tokens and old chats were not copied. User config is JSONC: parser needed skipped comments and trailing commas enabled. No private account values are included in this note.
- Supplying Environment replaces the entire child environment. Passing only two overrides broke native runtime startup on Windows; clone inherited variables before applying overrides. COPILOT_AUTO_UPDATE=false and COPILOT_TELEMETRY_DISABLED=true were used.
- Disabled session store, config discovery, on-demand instruction discovery, embedding retrieval, file-change tracking, custom instructions, skills, file hooks, host Git operations, session telemetry, memory and infinite sessions. Only mcp:cicd-get_run was available in the smoke; MCP server tools were independently restricted to get_run. Do not accidentally expose ambient CLI tools when expanding this.
- SessionConfig.McpServers used McpHttpServerConfig with Url and Tools. Its Type property is read-only. Some API members such as EnableMcpApps and direct PermissionDecision use were marked experimental GHCP001; avoid casual global suppression.
- Real SDK tool events reported cicd-get_run and a tool-call ID. ToolExecutionCompleteEvent.Data.Result contained source data suitable for later evidence validation. SendAndWaitAsync returned failed/a101 for run-102; session.idle was observed. These facts prove a minimal smoke, not a full investigator.
- During a second active turn, AbortAsync led to idle in about 38 ms; no additional tool calls appeared in a further 1000 ms observation. No copilot.exe remained after exit. Do not generalize this timing to browser disconnect detection.
- Even with memory/store disabled, runtime wrote seven temporary files, including .json/.lock/.log/.jsonl/.hold. Owned GUID directory cleanup after client disposal succeeded after both normal execution and auth failures. Never claim zero disk writes. Hard-crash cleanup remains unverified.
- Keep runtime/log/transcript files out of Git. Delete only application-owned paths, never the user's global Copilot directory or unrelated sessions.

## Still unverified and deferred

SignalR connection lifecycle, same-session follow-ups, fresh-session isolation, browser disconnect cancellation, report validation, saved report revisions, UI flow, all S01-S09 fixtures, six real evaluations and fresh-checkout acceptance were not implemented. No fake adapter result should stand in for these checks.

## Reference material

- Installed SDK README and XML documentation under the user's NuGet cache for version 1.0.28 were the exact API source.
- https://github.com/github/copilot-sdk/blob/main/dotnet/src/Client.cs documents runtime environment and Empty-mode keychain behavior; main can change.
- https://github.com/github/copilot-sdk/blob/main/docs/auth/authenticate.md
- https://github.com/github/copilot-sdk/blob/main/docs/setup/multi-tenancy.md

## Local data after rollback

The foundation Docker volume and user secrets are retained. The previously created synthetic investigator database can remain in that volume as inactive experimental data; the restored foundation health check connects to master and does not migrate, seed or query domain tables. No data volume, global credentials or unrelated local SQL instance is deleted as part of this code/history rollback.
