# Verification

Application shells are checked through builds and live health requests. Domain and protocol tests will be added with real behavior in Stage 2; no template-existence tests are included.

T03-03 verification: `dotnet test tests/CiCdInvestigator.Api.Tests -c Release` checks report validation without AI. After a Release solution build, `./scripts/Test-Reports.ps1` uses real Copilot SDK, SignalR, MCP and a dedicated SQL database for S01 and one insufficient-data fixture, then exercises lifecycle behavior. The smoke can submit one explicit user-style corrective follow-up after validation failure; the application has no automatic retry loop. See docs/development.md for results and limits.
