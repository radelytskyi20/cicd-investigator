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
