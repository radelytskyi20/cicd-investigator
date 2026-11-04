# API contracts

Status: SignalR investigation transport is accepted through Stage 3. The reduced Stage 4 REST/save/history implementation is ready for review; report revisions and catalog filters are deferred.

## Implemented read APIs (reduced Stage 4)

These user-facing reads reuse InvestigationQueries; the Investigator still reads facts through MCP. Success returns `{ status: "Ok", data, error: null }` with camelCase properties and string enums. Domain invalid input is 400, missing identity is 404, and execution failure is a generic 500 rather than empty data. No filtering or pagination is implemented for the tiny catalog.

| Method and route | Result |
| --- | --- |
| GET /api/runs | All runs, descending StartedAt then ID |
| GET /api/runs/{runId} | Run metadata and jobs/attempts |
| GET /api/runs/{runId}/baseline | Default qualified candidate and comparison limitations |
| GET /api/runs/{runId}/changes | Commit, stored patch/base revision and completeness |
| GET /api/runs/{runId}/jobs/{jobId}/logs?attempt=1&start=1&count=100 | Bounded positioned log page; count 1–200 |
| GET /api/runs/{runId}/jobs/{jobId}/tests?attempt=1 | Attempt-specific tests and completeness |

Run, changes, logs and tests accept optional `datasetVersion`. A supplied version that differs from the stored run returns 410 with an explicit unavailable-source message. This prevents substitution but is not a historical source archive. UI evidence links include that version and use the original `sourceLocator` line for log reads. Unavailable/truncated/empty source states remain within successful source results.

## Implemented session transport

SignalR implements start, follow-up, Stop and actual progress/tool events. The implemented T03-02 contract is recorded below. Automatic reconnect and session resume are excluded.

## Implemented completed-report storage (reduced Stage 4)

`answerReady.report` now includes `resultId` (a server-generated GUID). Only the latest validated result is saveable while the current SDK session is open and idle. A successful follow-up replaces it; Stop or validation failure can leave the previous result available. A fresh Start, disconnect or disposal clears it. No transient conversation or interrupted draft is stored.

- `POST /api/reports` accepts `{ "resultId": "<guid>" }`, not report JSON. Returns 200 with `{ id, savedAt, runId }` only after SQL succeeds. Repeating the same ID returns the same row. Busy, unknown or expired results return 409; malformed input is 400; storage failure is 500 and does not consume the ready result.
- `GET /api/reports` returns an array of `{ id, savedAt, runId, goal, outcome, summary }`, newest saved first.
- `GET /api/reports/{id}` returns `{ id, savedAt, runId, goal, result }`, where `result` is the stored ReadyReport. Unknown GUID is 404.

One SavedReports table stores immutable independent reports. No Investigation/ReportRevision hierarchy or revisions endpoint is implemented. Opening a report never creates or resumes an SDK session. Saved quotations remain in report JSON even when original sources become unavailable. The API and active agent share a gate so Save does not race a turn or replace its content with client input. This is a single-client local prototype, not a multi-user authorization boundary.

## Implemented single-client investigation transport (T03-02)

The local API exposes SignalR at `/investigation`. One singleton service owns one current SDK conversation and one active turn for the entire application. Multiple clients/tabs and parallel investigations are unsupported. SDK creation is lazy: ordinary startup and health checks do not make AI calls. `Mcp:Endpoint` comes from Aspire; `Copilot:CliPath` defaults to `copilot` on PATH, and `Copilot:Model` defaults to the locally verified gpt-5.4. Existing CLI authentication is reused.

### Client methods

- `Start({ runId, goal, baselineId? })`: after the previous turn ends, disposes the previous conversation/runtime and creates a fresh SDK conversation. Run and goal must be nonblank. Baseline is optional; T03-03 checks a supplied baseline against observed source data before answerReady.
- `FollowUp(message)`: sends a nonblank message to the current conversation. A missing conversation produces an error. Earlier messages and tool results remain available in the SDK session.
- `Stop()`: cancels the active turn and waits for cancellation processing; idle Stop is harmless. The current conversation remains available unless SDK cancellation fails and disposal is required. A later request must be explicit.

Start and FollowUp stay pending until the turn finishes. The client must await completion before starting another turn. A busy request is rejected with a HubException; there is no investigation queue. SignalR allows two concurrent method invocations solely so Stop can reach an active turn. No automatic reconnect or SDK resume is configured.

### Server events

All events use `investigationEvent` with `{ type, turnId, text?, toolName?, toolCallId?, report? }`. A new turn ID identifies each accepted request.

- `started`: SDK session is ready and the turn is about to be submitted.
- `toolStarted` / `toolCompleted`: actual SDK tool lifecycle events; tool call identity is preserved.
- `progress`: actual SDK idle notification; no estimated percentages or hidden reasoning.
- `answerReady`: validated compact report; see the T03-03 contract below. The earlier T03-02 conversational answer event is superseded.
- `error`: failure or missing session. Configuration/runtime failures use a generic user message; server logs record the technical error type without transcript data.
- `ended`: `text` is `completed`, `cancelled` or `error`. Cancellation suppresses queued progress/answer delivery before ended. Disconnected clients and a stopping host are not promised delivery of terminal events.

Disconnect cancellation uses the connection-aborted token and disposes the current session/client. Shutdown links active work to ApplicationStopping and awaits cleanup. SDK AbortAsync is followed by an idle wait; if cancellation does not settle within 15 seconds, the current SDK client/session is disposed. This is a cleanup fallback, not a configurable agent-work budget. CLI-managed runtime files are retained. There is no application conversation database, recovery or background completion.

T03-03 adds run eligibility, baseline validation, evidence validation and report semantics below.

## Compact validated reports (T03-03)

`answerReady` replaces the earlier conversational `answer` event. It carries `report: { report, durationMs, toolCallCount, inspectedSources }` on InvestigationEvent. The nested report has runId, optional baselineId, outcome (`supported`/`unknown`), causeCategory, summary, confidence (`low`/`medium`/`high`), confidenceReason, evidence, hypotheses, limitations and nextStep. FollowUp returns a full updated compact report of the same shape. durationMs and toolCallCount describe the current turn; inspectedSources lists tool names observed across the current investigation, including prior turns. No report is saved by this event.

Generated explanation prose is limited to 250 words, with a target of 120-200 words, 1-6 evidence items and 1-3 short hypotheses. Each hypothesis has explanation and zero-based supportingEvidence/opposingEvidence indexes into the report evidence array. The app may append short factual baseline-comparison limitations. Evidence quotations are separate from prose and are bounded to 1500 characters each; source text is never rewritten to make it fit.

Each evidence item has tool, runId, optional jobId/attempt, datasetVersion, locator (JSON Pointer into the observed MCP result), exact scalar quote and completeness. The server adds sourceLocator from the actual source: run identity, job/attempt, test identity or original log line number. Clients should use sourceLocator together with dataset/run/job/attempt identity when opening original data, not treat a paged JSON array index as a permanent log-line number. `get_run` citations describe complete metadata; log/test/change completeness is copied from sourceStatus.

Only successful native SDK MCP tool-result events populate the in-memory evidence set. Evidence can be reused within the active investigation and is cleared on a new Start or disposal. The validator checks JSON structure, compactness, hypothesis indexes, target eligibility, baseline eligibility and exact evidence membership including identity, version, quotation and completeness. It appends comparison caveats for unknown/different environment, configuration, version or job sets. Baseline eligibility means an earlier completed success of the same pipeline/ref; default candidate selection still comes from find_baseline. An explicit baseline must match the report baseline.

A validation error emits `error` followed by `ended(error)`, never answerReady, and keeps the SDK conversation available for an explicit corrective FollowUp or a fresh Start. There is no automatic model retry or repair loop. Execution errors still dispose the failed session. Stop/disconnect suppress report delivery as before. The validator proves citation provenance and structural consistency, not the semantic truth of a causal explanation; scenario evaluation remains necessary.
