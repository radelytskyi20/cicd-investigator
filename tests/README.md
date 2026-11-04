# Verification

Reduced Stage 4: after building and starting Aspire, `./scripts/Test-DemoApi.ps1` checks SQL-backed REST reads and minimum input/version guards. Supplying `-ResultId` for the current completed browser result also verifies idempotent Save and reopening. This is a live local smoke, not a fake SDK test. The actual browser S01/follow-up/Save/Stop/closure/restart results and limits are recorded in [Stage 4 review](../docs/stage-4-review.md); no broad browser suite is claimed.

Application shells are checked through builds and live health requests. Domain and protocol tests will be added with real behavior in Stage 2; no template-existence tests are included.

T03-03 verification: `dotnet test tests/CiCdInvestigator.Api.Tests -c Release` checks report validation without AI. After a Release solution build, `./scripts/Test-Reports.ps1` uses real Copilot SDK, SignalR, MCP and a dedicated SQL database for S01 and one insufficient-data fixture, then exercises lifecycle behavior. The smoke can submit one explicit user-style corrective follow-up after validation failure; the application has no automatic retry loop. See docs/development.md for results and limits.

T03-04 adds `./scripts/Test-Agent.ps1 -FollowUpOnly` for the focused S01/follow-up check. It prints report evidence and call/reuse counts for semantic review, then exits without rerunning the lifecycle scenarios. See docs/stage-3-review.md for the newly executed results versus reused accepted checks.
