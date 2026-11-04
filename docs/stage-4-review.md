# Stage 4 reduced demo review

Status: Ready for review. Owner acceptance of Done/G4 is pending.

## Approved scope

On November 4, 2026 the owner authorized a working local demo by 15:30 Europe/Budapest, reducing Stage 4 to the existing S01 failed/successful pair and a simple end-to-end path. This record concerns that reduced scope, not the original full feature list or Stage 5 acceptance.

Delivered: REST catalog/source reads, a one-screen React UI, real SDK/MCP investigation and visible tool events, compact report/evidence links, contextual follow-up, Stop, explicit idempotent Save and independent report history. SQL migration is applied by the existing MCP startup owner. No persistent application conversation or resume was added.

## Actual verification (November 4, 2026)

| Check | Observed result |
| --- | --- |
| Release solution build | Passed, zero warnings/errors; repeated after final API input-error handling change |
| Frontend type check and production build | Passed using pinned Node 24.22.1 and repository packages |
| Existing report validator tests | 18 passed, zero failures/skips |
| Official MCP protocol client | Six tools discovered and called; S01, baseline and existing edge checks passed |
| Test-DemoApi.ps1 | SQL-backed list, run/jobs, baseline, exact log line, tests and changes passed; unavailable version 410 and invalid Save 409 |
| Real browser S01 investigation | Validated report in 26.7 seconds, 8 tool calls, no corrective retry needed |
| Contextual cache-warning follow-up | Updated report in 8.3 seconds, 0 new tool calls, using previously inspected evidence |
| Evidence link | Opened run-101 / job-101-test-1 / attempt 1 / original line 3, not a paged array offset |
| Explicit Save and repeat Save | Initial and follow-up results saved independently; repeating the current result ID produced no duplicate; reopening retained the same ID |
| Stop in active real work | UI showed Stopped; server cancellation processing logged 41 ms |
| Close tab during fresh real investigation | Closed after get_run began; cancellation processing logged 26 ms; session/client disposed; no API-owned Copilot process remained |
| No implicit persistence | Exactly two explicitly saved reports remained after cancellation/closure; expired ready-result ID returned 409 |
| Complete AppHost stop/start | API, MCP, SQL and web became healthy; both saved reports survived; new browser showed no active investigation or conversation; saved follow-up reopened successfully |

The initial report attributed the midpoint failure to the stored change from AwayFromZero to default rounding, supported by tests and the baseline. The follow-up weakened the cache explanation because the warning was shared by both runs. Neither report claimed to execute a fix or independent reproduction.

One additional follow-up requested 30 repeated reads for a closure check; the agent instead returned a completed report saying those repeated reads were unnecessary. It was not saved. The actual closure check therefore used a fresh investigation and closed its tab after the first observed tool read. Do not count the unsaved response as successful cancellation evidence.

## Boundaries

- Cancellation timings measure server abort processing after detection, not network-loss detection or a guarantee of immediate cancellation.
- Stop kept the active context; tab closure disposed it. CLI-managed runtime files remain acceptable under Stage 3 decisions.
- No broad automated browser suite, multi-client testing, database-write failure injection, repeated AI scenario evaluation or negative UI scenario is claimed.
- Saved reports are independent snapshots, not grouped revisions. Catalog filters, manual baseline UI, rich source viewers and old-source reconstruction remain deferred.
- The production seed remains S01 only (run-102 failed, run-101 succeeded). Stage 5 S01–S09/repeated evaluation remains unimplemented and unapproved.
- The model remains nondeterministic. A validation error is visible and may need an explicit corrective follow-up; there is no automatic model retry.

## Demo

Use the web URL reported by Aspire (ports change on restart). Select run-102, click Investigate failure, open evidence, ask whether the cache warning explains the failure, then Save report. Reopen from Saved reports. run-101 remains readable as a successful baseline. See demo.md and development.md for commands and expected behavior.
