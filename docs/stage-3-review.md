# Stage 3 verification review

Status: Done. Both T03-04 subtasks and the Stage 3 closeout were accepted by the owner on November 4, 2026. Scope: one local client, sequential investigations, existing Copilot login, compact reports and active-session context.

## Evidence reused from accepted tasks

These checks were not rerun for T03-04. Their original results remain in [development](development.md).

| G3 requirement | Existing verification evidence | Scope and limits |
| --- | --- | --- |
| Real tools and SDK loop | T03-01-01, commit daa1fda: installed CLI/login, native MCP call, result payload, final answer and idle | Real SDK 1.0.28 / CLI 1.0.105; not a fake adapter |
| Same-session memory and fresh-session isolation | T03-01-02, commit 41be673: random marker recall, distinct new session with no marker; T03-02, commit 6096a80: message and prior tool-result recall over SignalR | No automatic resume; no multi-client support required |
| Stop, disconnect and disposal | T03-02 and T03-03, commits 6096a80 and 3189b2c: active Stop, explicit continuation, graceful close, WebSocket abort, host shutdown and child-process exit | Bounded local observations; no hard-crash or arbitrary network-partition guarantee |
| Evidence-validated compact reports | T03-03, commit 3189b2c: 18 deterministic validator tests and real S01/insufficient-data reports | Citation provenance and structure are checked; causality and prose still need review |
| Errors do not publish ready results | T03-03: invalid successful target and incorrect report target rejected; explicit corrective follow-up succeeded | Test-client correction was explicit; no automatic production repair loop |
| No application draft/chat persistence | Current API service keeps SDK session/evidence in memory and clears them on new Start/disposal; no report/chat write path exists | CLI-managed service files may remain as explicitly approved; Save/history is Stage 4 |

## New focused check

Executed on November 4, 2026 with SDK 1.0.28, CLI 1.0.105, model gpt-5.4 and the existing SQL-backed MCP host. The existing smoke now supports `./scripts/Test-Agent.ps1 -FollowUpOnly`, which exits after S01 and the substantive follow-up. It checks evidence coverage and prints the completed report for semantic review. Keyword/coverage checks are not treated as proof of correct reasoning. A fixed tool-call sequence or zero-call follow-up is not required.

### New execution results and semantic review

- Build: `dotnet build tests/CiCdInvestigator.Agent.Smoke -c Release --no-restore --verbosity minimal` passed with zero warnings/errors, including the API dependency.
- First focused run: S01 used six calls (21450 ms); follow-up used zero calls (6818 ms) and reused four exact citations. Structural checks passed, but semantic review found that the report called additional, unrequested diagnostics unavailable. This was not treated as a successful source-availability check.
- Correction: one domain-instruction clarification now explicitly distinguishes not inspected/not retrieved from empty, not found, unavailable and truncated. No validator, lifecycle or tool implementation changed.
- Final focused run: S01 used eight calls (22862 ms); follow-up used zero calls (10334 ms). Two citations were identical to those in the initial report; two further citations came from log results already read during the initial investigation but not quoted in its initial report. Thus the follow-up used SDK conversation/tool context, not merely the previous report.
- Neither focused run required a validation correction or test-client retry. Both completed with validated compact reports. No authentication or quota failure occurred.
- The final follow-up describes the cache warning as an alternative without evidence linking it to the numeric mismatch. It connects the failure to the rounding patch and passing baseline, qualifies the conclusion and recommends a verifying rerun rather than claiming a fix was tested.
- Source review: cache warning is original line 2 of job-102-test-1, attempt 1; the failed assertion is line 3; the baseline citation is test-101-round, attempt 1; the patch is from run-102. All references are mock-v1 and were validated against actual returned MCP data.
- The final limitation says cache diagnostics and an independent rerun were not retrieved. It no longer asserts that unrequested sources are unavailable. This focused example passed semantic review; it is not a universal guarantee about model prose.

Short excerpt from the final generated report:

> The cache warning is unlikely to explain this failure: the log says the build continued without cache, while the test explicitly reports a rounding mismatch for 1.005.

The new check closes the identified follow-up gap. Within the approved single-client local scope, no unresolved G3 acceptance blocker was found. General model-quality limitations remain below. No new full-suite, negative-case or lifecycle pass is claimed after the prompt clarification; those results are explicitly reused above.

## Contract and responsibility review

The SignalR methods remain Start, FollowUp and Stop. A validated result is answerReady with the compact report, current-turn duration/tool count and investigation-wide observed tool names. The earlier plain answer event is superseded. Rejected validation retains the conversation for explicit retry; execution failure disposes it. Disconnected clients are not promised terminal-event delivery. The contract was checked against the implemented event/report records and service flow; stale planned wording and the missing optional report event field were corrected in [API contracts](api-contracts.md).

Copilot SDK/CLI owns model turns, tool selection and continuation after tool results. The application supplies domain instructions, six read-only MCP tools, source capture, report validation, SignalR events and lifecycle control. SQL-backed MCP returns facts. The application has no second LLM loop, background investigation queue or saved-conversation recovery.

## Remaining boundaries

Browser UI and tab behavior through that UI, report Save/history, hard-crash cleanup, cancellation fallback under forced runtime failure, and repeated scenario evaluation are not claimed here. They are later scope or documented prototype limits. Stage 5 owns the three independent primary/negative evaluation runs. Stage 4 must not start until separately authorized. T03-04 and G3 are accepted (Done) following owner acceptance of both closeout subtasks on November 4, 2026.
