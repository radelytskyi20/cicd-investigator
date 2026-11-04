# Development

## Verified environment (November 4, 2026)

T01-01 establishes the prerequisite baseline; applications do not exist yet.

| Component | Selected version | Verification |
| --- | --- | --- |
| .NET SDK | 10.0.401 | Installed; global.json disables roll-forward |
| Aspire CLI / AppHost SDK | 13.4.6 | CLI installed; hosting packages restore |
| Aspire hosting packages | 13.4.6 | AppHost, SqlServer and JavaScript restored together |
| EF Core SQL Server | 10.0.14 | Restored for net10.0 |
| Official MCP ASP.NET Core SDK | 2.3.1 | Restored for net10.0 |
| GitHub.Copilot.SDK | 1.0.28 | Package metadata pins CLI 1.0.105 |
| Copilot CLI | 1.0.105 | Installed; authentication and AI calls not tested |
| Docker | 28.3.0 | Daemon responds; Linux containers |
| Node.js | 24.22.1 | Existing Codex runtime; .node-version pin |
| npm | 10.9.2 | Existing system npm works with selected Node |
| React / React DOM | 19.3.1 | Registry versions verified; application build pending |
| TypeScript | 7.0.2 | Registry version verified; application build pending |
| Vite / React plugin | 8.3.5 / 6.2.1 | Registry engine and peer requirements checked |

.NET packages are centrally pinned in Directory.Packages.props. AppHost SDK must use 13.4.6 when scaffolded. Frontend versions above must become exact package references and an npm lockfile in T01-02. These are selected baselines, not evidence of a working application.

The system Node is 23.7.0, which is not the required LTS baseline. No software was installed or upgraded. For this machine, select the already available LTS executable for the current PowerShell session:

```powershell
$env:PATH = "$env:USERPROFILE\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin;C:\Program Files\nodejs;$env:PATH"
node --version
npm.cmd --version
```

This location belongs to Codex and may change with its updates; it is not a portable repository dependency. Other machines need Node 24.22.1 available on PATH. The version file does not install or select Node by itself.

## Executed prerequisite checks

```powershell
dotnet --list-sdks
dotnet --version
aspire --version
copilot --version
docker info --format '{{.ServerVersion}} {{.OSType}}'
git status --short
git diff --check
```

Restricted execution initially denied access to Docker configuration and its named pipe. Repeating the read-only check with normal host access returned `28.3.0 linux`. System npm was also available with normal host access. These were execution-context issues, not missing installations.

A disposable net10.0 project under ignored .scratch/version-check referenced the six selected .NET packages and consumed the central version file. `dotnet restore .scratch/version-check/Compatibility.csproj --verbosity minimal` succeeded. The initial restore reported vulnerable transitive MessagePack 2.5.302 and System.Security.Cryptography.Pkcs 10.0.8. Central transitive pins to 2.5.303 and 10.0.14 removed those warnings on the repeated restore. The subsequent `dotnet list .scratch/version-check/Compatibility.csproj package --vulnerable --include-transitive` reported no vulnerable packages in the current sources. No application was built, no SDK session was started, and Copilot runtime downloading was disabled for this metadata check.

## Version evidence

- [Aspire AppHost 13.4.6](https://www.nuget.org/packages/Aspire.Hosting.AppHost/13.4.6): retained to match the installed CLI.
- [EF Core SQL Server 10.0.14](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.14) and [MCP ASP.NET Core 2.3.1](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/2.3.1): package resolution verified against net10.0.
- [Copilot SDK 1.0.28](https://www.nuget.org/packages/GitHub.Copilot.SDK/1.0.28): downloaded package build/GitHub.Copilot.SDK.props specifies CopilotCliVersion 1.0.105, exactly matching the installed CLI. Runtime authentication, tool calls and lifecycle remain T03-01 checks. Reuse the installed runtime when integration is implemented; do not silently download another CLI.
- [MessagePack advisory](https://github.com/advisories/GHSA-qhrr-8q5h-9q3h) and [PKCS advisory](https://github.com/advisories/GHSA-846f-f764-76mx): reasons for central transitive patch pins.
- npm registry metadata was queried with `npm.cmd view` for the selected React, React DOM, TypeScript, Vite and React plugin versions. Vite and its plugin require Node ^20.19.0 or >=22.12.0; selected Node 24 satisfies that constraint.

## Planned startup and checks

T01-02 verifies actual backend/frontend builds and host health. T01-03 verifies AppHost startup, shutdown, restart, endpoint discovery, EF connectivity and volume retention. There is no application startup command yet.

SQL Server will publish configurable host port 14330, never host port 1433, while keeping container port 1433. Check availability again at startup; a point-in-time port check cannot reserve it. Keep the existing local SQL Server untouched. No database container or image was created in T01-01.

## Secrets and runtime state

Keep credentials in local secrets; never commit runtime session files. Copilot authentication, actual SDK integration and service-file behavior remain unverified until T03-01. Ignore rules do not prove cleanup or disable persistent memory.

## T01-02 verified application shell commands

- Build backend: `dotnet build CiCdInvestigator.slnx --verbosity minimal`.
- Build frontend from its directory: `node C:/Program Files/nodejs/node_modules/npm/bin/npm-cli.js run build` (quote the path containing spaces). Invoke npm-cli.js with selected Node explicitly: npm.cmd uses its adjacent system Node on this machine, even when PATH selects Node LTS.
- API: `dotnet run --no-build --project src/CiCdInvestigator.Api --urls http://127.0.0.1:5100`.
- MCP host: `dotnet run --no-build --project src/CiCdInvestigator.McpServer --urls http://127.0.0.1:5101`.
- Frontend: `node node_modules/vite/bin/vite.js --host 127.0.0.1` from its directory.
- Live /health checks returned Healthy on both hosts. Browser at http://127.0.0.1:5173 displayed API connected through the Vite proxy. No domain MCP endpoint exists yet.
- The T01-01 temporary compatibility folder was deleted at the owner's request; its historical checks are retained above.

## T01-03 local orchestration

Set a strong SQL password once in AppHost user secrets (Parameters:sql-password). Keep the same secret when reusing the volume. The AppHost Development profile loads it; never commit the value. The first generated-password experiment used cicd-investigator-sql-data, which is retained unused after a restart login mismatch. The working named volume is cicd-investigator-data. No old volume was deleted.

```powershell
# Use Node 24.22.1 on PATH, or pass its executable explicitly:
./scripts/Start-Local.ps1 -NodePath "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe"
# Equivalent direct startup, with the selected Node already on PATH:
aspire run --detach --format Json --non-interactive
aspire describe --apphost src/CiCdInvestigator.AppHost/CiCdInvestigator.AppHost.csproj --format Json
aspire stop --apphost src/CiCdInvestigator.AppHost/CiCdInvestigator.AppHost.csproj --non-interactive
```

The script validates the Node pin; direct Aspire startup and stop were exercised. Aspire supplies dynamic API, MCP and frontend URLs; use describe/dashboard instead of hard-coding them. SQL's stable external endpoint is localhost:14330 (override with Sql__Port). Aspire proxies that endpoint to Docker's dynamically mapped container port 1433. Host port 1433 and invalid/occupied ports are rejected before resources start. The container image is pinned to mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04, verified in Microsoft's registry.

The MCP /health check performs an EF Core connection to master until domain migrations exist. SQL readiness does not depend on future domain tables. API waits for MCP, frontend waits for API. AddNodeApp runs Vite with the selected Node executable; Aspire manages its process and endpoint. The dashboard uses HTTPS and its generated login token. Never commit that token or full resource/environment dumps.

The named volume survives stop/restart. Aspire's stop command can return shortly before container cleanup finishes; wait for cleanup before restarting if a port remains occupied. The installed DCP emitted notification-socket warnings on Windows; resources and health checks still ran, so these warnings are recorded rather than mistaken for application success or failure.

Use `./scripts/Stop-Local.ps1` for a stop that waits for SQL container shutdown before a restart. Immediate stop/start exposed a SQL instance-lock race; restarting the SQL resource after old container cleanup recovered it. Subsequent API/MCP health and frontend proxy requests all returned 200 with one SQL container and one MCP process. The named volume and stable password survived. G1 is submitted for review with this observed runtime limitation.

## Foundation scope restored (November 4, 2026)

At the owner's request the active code/history was returned to the five approved foundation slices at ac404ef. Stage 2-3 implementation branches, domain migrations/queries/tests and the SDK smoke were removed. Historical research is retained in [deferred-stage-2-3.md](research/deferred-stage-2-3.md); it is not current implementation or authorization to proceed.

After removing stale generated .NET artifacts, the solution build passed with zero warnings/errors; the selected Node 24.22.1 frontend type check and production build passed. The project stack was stopped with Stop-Local and restarted through Aspire. SQL was healthy on the configured 14330 endpoint, API and MCP /health returned 200, and the frontend /api/status proxy returned ready. One project SQL container and one MCP process were observed. The deferred /mcp route returned 404 as expected for the shell.

SQL volumes and local secrets were retained. The volume may still contain the earlier synthetic investigator database; the restored EF health check uses master and neither reads domain tables nor applies migrations. No Copilot analysis runs at startup. Foundation tasks retain their previous review statuses; later-stage tasks are Planned and require a new small-task breakdown and approval.

## T02-01 data checks

The MCP host now migrates and seeds investigator-prototype before /health can report ready; health reads the initialized run instead of connecting to master. The old synthetic database is retained untouched. The dedicated investigator-smoke-t0201 database is retained for inspection and contains only test fixtures.

With the Aspire stack running, execute `./scripts/Test-Data.ps1`. It reads the existing local AppHost secret without printing it, discovers the container's published IPv4 SQL port, and runs the xUnit SQL integration check. A first attempt through localhost:14330 timed out; the published container port passed. This script uses the documented Windows local secrets location. On other machines set INVESTIGATOR_TEST_SQL to a dedicated test database and run `dotnet test tests/CiCdInvestigator.Data.Tests --verbosity minimal`.

EF migration generation used `.scratch/tools/dotnet-ef migrations add InitialDataset --project src/CiCdInvestigator.Data`, with dotnet-ef 10.0.14 installed under ignored .scratch/tools. Microsoft.EntityFrameworkCore.Design is pinned to 10.0.14. Test packages are pinned to available xUnit 2.9.3, runner 3.0.2 and Microsoft.NET.Test.Sdk 17.12.0. Test failures remain failures when no SQL connection is supplied; they are not silently skipped.

Verified T02-01 results: solution build passed with zero warnings/errors after stopping running binaries. The SQL integration test passed on fresh initialization and again after full Aspire stop/start. After restart, MCP and API /health returned 200 and the frontend /api/status proxy returned ready. Frontend source was unchanged; no new browser interaction or SDK/MCP protocol verification is claimed.

## T02-02 query verification

Run `./scripts/Test-Data.ps1` with the local Aspire SQL container running. Both tests passed: the original data smoke and the query smoke. Coverage includes filtered S01 reads, unknown/invalid inputs, job-attempt isolation, bounded and source-truncated logs, missing tests, stored diff scope, empty versus unavailable changes, absent baseline and qualified noncomparable baseline. Edge-case changes are test-local and rolled back; no extra demonstration scenarios were seeded. No MCP protocol, SDK or new browser verification is claimed.

The full solution build passed with zero warnings/errors after stopping the running stack to release binaries. Aspire was started again afterward. git diff --check passed.

## T02-03 protocol smoke

Run ./scripts/Test-Mcp.ps1 with one healthy Aspire MCP host. The script discovers its HTTP URL, appends /mcp and runs the console smoke using ModelContextProtocol.Core 2.3.1. No SQL credentials or AI calls are needed by the client. The solution build passed with zero warnings/errors. The real client discovered exactly six tools and called all of them; it checked S01 evidence/baseline and distinct unknown-ID, invalid-range and unavailable-source results. T02-04 partial-source verification on a dedicated database remains Planned.


After one full Aspire stop/start, all resources became Healthy and the same protocol smoke passed again. One MCP process was observed. An immediate smoke attempt before readiness correctly refused to run; it passed once the host was Healthy.

## T02-04 / G2 verification (November 4, 2026)

With the solution built and Aspire SQL running:

- `./scripts/Test-McpIntegration.ps1` — passed: existing six-tool protocol smoke plus one xUnit test for partial logs and distinct attempts on investigator-mcp-test.
- `./scripts/Test-Mcp.ps1` — passed: ordinary Aspire demonstration path.
- `./scripts/Test-Data.ps1` — passed: both existing SQL tests; now filters out the separately configured MCP category.
- Changed test project and dependencies built during dotnet test without build errors or warnings. No production code changed in T02-04.

The integration script uses the existing MCP host Debug/net10.0 executable, local AppHost secret and discovered container port. Build the solution before using it on a fresh checkout. It creates or reuses the named synthetic test database, starts a temporary host on loopback, waits for readiness and always stops its owned process. It leaves the test database for inspection; no test cleanup deletes databases or volumes. The extra test fixture is idempotent and remains outside the demonstration database. Host logs are ignored under .scratch/mcp-integration. The script never prints credentials.

The MCP xUnit test requires INVESTIGATOR_MCP_TEST_SQL and INVESTIGATOR_MCP_TEST_URL; use the script to configure both. Test-Data remains the entry point for ordinary data tests. G2 was accepted by the owner on November 4, 2026. No Copilot authentication, agent loop or session-memory validation is claimed.

## T03-01-01 real SDK/MCP verification

Build the API with `dotnet build src/CiCdInvestigator.Api -c Release --verbosity minimal`, then run `./scripts/Test-Copilot.ps1` with the Aspire MCP host healthy. The script discovers MCP and the installed CLI, then invokes an explicit smoke mode in the API executable. Ordinary API startup does not start AI work. The optional `-Model` parameter defaults to gpt-5.4, verified on this machine.

SDK 1.0.28 with installed CLI 1.0.105 reused the existing login directly in CopilotCli mode. No token copying, login flow, custom home directory or runtime download was needed. AvailableTools permits only mcp:cicd-get_run; the MCP configuration permits only get_run. Config discovery and skills are disabled so repository developer instructions do not become investigator instructions. Cross-session store and persistent memory are disabled; ordinary CLI files remain acceptable.

Actual run: cicd-get_run returned source data for run-102; the final answer reported failed at revision-102. ToolExecutionStartEvent exposes name/call identity, ToolExecutionCompleteEvent.Data.Result exposes source data, and SessionIdleEvent plus SendAndWaitAsync indicate completion. The smoke checks the known fixture identity/revision and failed outcome; this is not general report validation. The two-minute wait is a smoke timeout, not an application work budget or proof of cancellation. Explicit cancellation and fresh-session checks belong to T03-01-02.

## T03-01-02 lifecycle verification

Run `./scripts/Test-Copilot.ps1 -Lifecycle` after the same API Release build, with the Aspire MCP host healthy. This uses real SDK/CLI calls and the existing login, not a fake adapter. The minimal experiment remains inside the API executable; ordinary startup does not run it.

Verified November 4, 2026 with SDK 1.0.28, installed CLI 1.0.105 and gpt-5.4:

- A random marker was recalled in the same SDK session without repeating it in the follow-up.
- Cancellation was requested after the first real tool-start event, while a multi-read turn was active. AbortAsync reached session.idle in 45 ms. The observed tool-start count stayed at one and no new assistant messages arrived during the subsequent three-second observation.
- After disposing that session, CreateSessionAsync produced a different session ID. Its event history contained no prior marker and the model answered UNKNOWN when asked for it without supplying it.
- Both sessions and the client completed normal await-using disposal. A read-only Windows process observation identified copilot.exe PID 28404 as a direct child of smoke API PID 11484; after completion neither that child nor any direct children of the API remained. No unrelated process was stopped.

The three-second observation is bounded evidence, not a guarantee about arbitrary network failures. Browser disconnect, SignalR, host shutdown and hard crashes remain later lifecycle work. The check does not prove cancellation of an already dispatched HTTP request inside the MCP host. SDK disposal closes in-memory sessions but CLI-managed disk files can remain; no custom cleanup, global settings changes, transcript persistence in the application or automatic resume was added.

## T03-02 single-client SignalR verification

Build with `dotnet build CiCdInvestigator.slnx -c Release --verbosity minimal`, then run `./scripts/Test-Agent.ps1` with the Aspire MCP host healthy. The new smoke uses the pinned official Microsoft.AspNetCore.SignalR.Client 10.0.14 and hosts the same API on an ephemeral loopback address. It connects to the existing SQL-backed MCP and uses the real SDK/CLI and current login. No frontend or fake AI adapter is involved. The API can also run normally under Aspire; it receives Mcp:Endpoint and resolves Copilot:CliPath (default copilot on PATH) and Copilot:Model (default gpt-5.4). The smoke explicitly supplies the installed executable path.

Verified on November 4, 2026:

- Full Release solution build passed with zero warnings/errors. The final changed smoke project rebuilt with zero warnings/errors.
- Real SignalR start returned the MCP-backed revision. A follow-up recalled both a random user marker and the prior tool result without new tool calls. A subsequent Start did not inherit that marker.
- A missing session reported an error. An overlapping request was rejected; the expected HubException is logged by SignalR. No investigation queue was added.
- Stop reached SDK idle in 28 ms, returned cancelled without an answer, and emitted no further events in a one-second observation. Idle Stop was harmless; a later explicit follow-up retained the conversation.
- Graceful disconnect cancelled in 38 ms and cleared the session. Disconnect/reconnect verification completed in 1544 ms, including a deliberate 1500 ms observation wait; this is not an isolated browser detection-latency measurement.
- Abrupt WebSocket abort cancelled in 12 ms and cleared the session; reconnect was explicit and FollowUp required a new Start.
- Host shutdown during active work cancelled in 19 ms and completed normal session/client disposal. A subsequent Windows process query found no children of the smoke process (PID 16312). No unrelated process was stopped.

During development, the first smoke treated the expected shutdown WebSocket exception as a failure. Another run attempted reconnect before CLI disposal finished; its checker waited for an event from a rejected invocation. The checker now handles expected transport closure and bounds its wait for cleanup; the final full smoke passed. A build attempted while that earlier checker held its executable open failed with a Windows file-lock error; after stopping that owned checker, the build passed.

Limitations: no browser/UI, hard-crash cleanup, network partition timing, general report validation or G3 acceptance is claimed. These are local real-integration checks, not the repeated scenario evaluation in T05-02. The cancellation fallback was not forced. SDK files remain CLI-managed as approved.

## T03-03 compact report checks

Commands verified on November 4, 2026:

- `dotnet build CiCdInvestigator.slnx -c Release --verbosity minimal` — passed, zero warnings/errors.
- `dotnet test tests/CiCdInvestigator.Api.Tests -c Release --no-restore --verbosity minimal` — 18 passed. Checks cover malformed JSON, evidence quotation/tool/run/job/attempt/version/locator/completeness mismatches, evidence reuse versus new-investigation isolation, unsupported target/baseline, hypothesis indexes, original log-line resolution, baseline limitations, unknown cause and excessive report length.
- `./scripts/Test-Reports.ps1` — final real SignalR/SDK/MCP smoke passed against the isolated investigator-report-test database. Requires the existing Aspire SQL container and a Release build. It reuses the local SQL secret without printing it, starts an owned MCP host, and retains the synthetic database. Only that database receives the minimal insufficient-data fixture. The ordinary S01 demonstration database is unchanged.

The final S01 report used six tool calls in about 22.3 seconds, cited the failed test and rounding patch, and considered the cache warning. The negative report used four calls in about 17.0 seconds, returned unknown cause, and cited truncated/unavailable sources. These are individual integration observations, not the repeated Stage 5 evaluation. Native SDK tool-result Content supplied parsable source JSON; no bridge or alternate SQL path for the investigator was required.

Follow-up produced an updated validated report. A successful target was rejected. Following the cancellation exercise, the model initially returned a report for the wrong target; validation emitted an error without answerReady. The smoke client submitted one explicit corrective follow-up, and the corrected report passed. The smoke may perform at most one such user-style corrective request per report; the application itself never retries automatically. Earlier development runs also rejected inaccurate citations, demonstrating that model output is not assumed valid. A validation failure is a visible result, not silently repaired or saved.

Stop, explicit continuation, graceful disconnect, abrupt WebSocket loss and shutdown passed with the report contract. Final cancellation-processing observations were 22/37/11/15 ms respectively; disconnect/reconnect took 1525 ms including the deliberate 1500 ms wait. No child runtime remained after smoke PID 18400 exited. CLI-managed files were left untouched. Restricted builds could not read the user's NuGet settings; the final build with normal host access passed.

The real smoke prints only completed synthetic reports, not credentials or full SDK session dumps. API errors retain the conversation for explicit correction, while a new Start/disposal clears its evidence. UI rendering, Save/history, broad semantic evaluation, crash cleanup and G3 acceptance remain unverified or later scope. Source membership validation does not prove every sentence or hypothesis is correct; in particular, distinguishing uninspected sources from unavailable ones remains part of scenario review.

## T03-04 focused follow-up and G3 submission

New command: `./scripts/Test-Agent.ps1 -FollowUpOnly` runs only S01 plus its substantive cache-warning follow-up, then closes the client/host. It retains the existing explicit single corrective-follow-up capability but does not add application retries. The smoke checks validated warning/change/failure evidence and prints the answer for semantic review. Identical-citation reuse and new tool-call counts are reported, not forced.

Changed project build: `dotnet build tests/CiCdInvestigator.Agent.Smoke -c Release --no-restore --verbosity minimal` passed with zero warnings/errors. The first focused run passed structural checks but incorrectly described unrequested diagnostics as unavailable. A one-sentence domain-instruction clarification fixed the distinction. The final focused run passed semantic review: eight initial tool calls in 22862 ms; zero follow-up calls in 10334 ms; two citations repeated from the first report and two additional citations from already-read log results. No validation retry or authentication/quota failure occurred in either focused run.

Existing accepted lifecycle, isolation, validator and negative-case results were reused, not rerun. No fake SDK framework was added. The scope and evidence mapping, precise source references, contract audit and remaining limits are recorded in [Stage 3 review](stage-3-review.md). T03-04 and G3 are Ready for review, not owner-accepted Done. Stage 4 has not started.
