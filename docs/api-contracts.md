# API contracts

Status: Planned. No HTTP routes, hub, DTOs or status codes are implemented or verified.

## Planned read APIs

T04-01 will document catalog filters, run/job/log/test/change/baseline reads, source identity, completeness and distinct error responses. User-facing reads must not replace the Investigator's MCP access.

## Planned session transport

SignalR is proposed for start, follow-up, Stop and actual progress/tool events. T03-02 will freeze message and event contracts, session ownership, cancellation and disconnect behavior. Automatic reconnect and session resume are excluded.

## Planned completed-report storage

T03-03 will define validated report output. T04-01 will define ready-result identity, explicit idempotent Save, revisions, report history and write failures. No transient conversation or interrupted draft is saved. See [requirements](requirements.md).
