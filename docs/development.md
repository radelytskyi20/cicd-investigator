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
