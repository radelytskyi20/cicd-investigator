# API contracts

Status: SignalR investigation transport is implemented in T03-02 and accepted (Done). Catalog REST APIs and completed-report storage remain planned.

## Planned read APIs

T04-01 will document catalog filters, run/job/log/test/change/baseline reads, source identity, completeness and distinct error responses. User-facing reads must not replace the Investigator's MCP access.

## Planned session transport

SignalR implements start, follow-up, Stop and actual progress/tool events. The implemented T03-02 contract is recorded below. Automatic reconnect and session resume are excluded.

## Planned completed-report storage

T03-03 will define validated report output. T04-01 will define ready-result identity, explicit idempotent Save, revisions, report history and write failures. No transient conversation or interrupted draft is saved. See [requirements](requirements.md).

## Implemented single-client investigation transport (T03-02)

The local API exposes SignalR at `/investigation`. One singleton service owns one current SDK conversation and one active turn for the entire application. Multiple clients/tabs and parallel investigations are unsupported. SDK creation is lazy: ordinary startup and health checks do not make AI calls. `Mcp:Endpoint` comes from Aspire; `Copilot:CliPath` defaults to `copilot` on PATH, and `Copilot:Model` defaults to the locally verified gpt-5.4. Existing CLI authentication is reused.

### Client methods

- `Start({ runId, goal, baselineId? })`: after the previous turn ends, disposes the previous conversation/runtime and creates a fresh SDK conversation. Run and goal must be nonblank. Baseline is optional context, not a validated baseline selection in this task.
- `FollowUp(message)`: sends a nonblank message to the current conversation. A missing conversation produces an error. Earlier messages and tool results remain available in the SDK session.
- `Stop()`: cancels the active turn and waits for cancellation processing; idle Stop is harmless. The current conversation remains available unless SDK cancellation fails and disposal is required. A later request must be explicit.

Start and FollowUp stay pending until the turn finishes. The client must await completion before starting another turn. A busy request is rejected with a HubException; there is no investigation queue. SignalR allows two concurrent method invocations solely so Stop can reach an active turn. No automatic reconnect or SDK resume is configured.

### Server events

All events use `investigationEvent` with `{ type, turnId, text?, toolName?, toolCallId? }`. A new turn ID identifies each accepted request.

- `started`: SDK session is ready and the turn is about to be submitted.
- `toolStarted` / `toolCompleted`: actual SDK tool lifecycle events; tool call identity is preserved.
- `progress`: actual SDK idle notification; no estimated percentages or hidden reasoning.
- `answerReady`: validated compact report; see the T03-03 contract below. The earlier T03-02 conversational answer event is superseded.
- `error`: failure or missing session. Configuration/runtime failures use a generic user message; server logs record the technical error type without transcript data.
- `ended`: `text` is `completed`, `cancelled` or `error`. Cancellation suppresses queued progress/answer delivery before ended. Disconnected clients and a stopping host are not promised delivery of terminal events.

Disconnect cancellation uses the connection-aborted token and disposes the current session/client. Shutdown links active work to ApplicationStopping and awaits cleanup. SDK AbortAsync is followed by an idle wait; if cancellation does not settle within 15 seconds, the current SDK client/session is disposed. This is a cleanup fallback, not a configurable agent-work budget. CLI-managed runtime files are retained. There is no application conversation database, recovery or background completion.

T03-03 adds run eligibility, baseline validation, evidence validation and report semantics below.

## Compact validated reports (T03-03)

`answerReady` replaces the earlier conversational `answer` event. It carries `report: { report, durationMs, toolCallCount, inspectedSources }` on InvestigationEvent. The nested report has runId, optional baselineId, outcome (`supported`/`unknown`), causeCategory, summary, confidence (`low`/`medium`/`high`), confidenceReason, evidence, hypotheses, limitations and nextStep. FollowUp returns a full updated compact report of the same shape. No report is saved by this event.

Generated explanation prose is limited to 250 words, with a target of 120-200 words, 1-6 evidence items and 1-3 short hypotheses. Each hypothesis has explanation and zero-based supportingEvidence/opposingEvidence indexes into the report evidence array. The app may append short factual baseline-comparison limitations. Evidence quotations are separate from prose and are bounded to 1500 characters each; source text is never rewritten to make it fit.

Each evidence item has tool, runId, optional jobId/attempt, datasetVersion, locator (JSON Pointer into the observed MCP result), exact scalar quote and completeness. The server adds sourceLocator from the actual source: run identity, job/attempt, test identity or original log line number. Clients should use sourceLocator together with dataset/run/job/attempt identity when opening original data, not treat a paged JSON array index as a permanent log-line number. `get_run` citations describe complete metadata; log/test/change completeness is copied from sourceStatus.

Only successful native SDK MCP tool-result events populate the in-memory evidence set. Evidence can be reused within the active investigation and is cleared on a new Start or disposal. The validator checks JSON structure, compactness, hypothesis indexes, target eligibility, baseline eligibility and exact evidence membership including identity, version, quotation and completeness. It appends comparison caveats for unknown/different environment, configuration, version or job sets. Baseline eligibility means an earlier completed success of the same pipeline/ref; default candidate selection still comes from find_baseline. An explicit baseline must match the report baseline.

A validation error emits `error` followed by `ended(error)`, never answerReady, and keeps the SDK conversation available for an explicit corrective FollowUp or a fresh Start. There is no automatic model retry or repair loop. Execution errors still dispose the failed session. Stop/disconnect suppress report delivery as before. The validator proves citation provenance and structural consistency, not the semantic truth of a causal explanation; scenario evaluation remains necessary.
