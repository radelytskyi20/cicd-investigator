# Local demonstration

Status: The reduced Stage 4 real browser path is verified and Ready for review. G4 acceptance remains the owner's decision. Full Stage 5 evaluation is not included.

## Before the demo

Start through `scripts/Start-Local.ps1` with the pinned Node executable as documented in development.md. Wait for SQL, MCP, API and web to be healthy. Open the web resource HTTP URL from the Aspire dashboard/describe output; the port changes on restart. Existing local Copilot login is reused. Keep only one investigation tab open.

## Five-minute walkthrough

1. Show the two runs: run-102 failed, run-101 succeeded. They form the S01 calculation-regression example.
2. Select run-102. Open Logs, Tests or View changes to show the source data; the successful baseline is run-101.
3. Click Investigate failure with the default goal. Actual MCP tool events appear while Copilot runs the agent loop. The verified first answer took about 27 seconds; timing and model output vary.
4. Read the compact finding, confidence, limits and next check. Click an evidence link to open its original source. Log links preserve the original run/job/attempt and line number.
5. Ask: `Could the cache warning explain the failure? Use the evidence already inspected and update the compact report.` The verified follow-up reused session evidence with zero new tool calls; further reads are still allowed.
6. Click Save report. A saved indicator appears only after the API writes the completed result. Open it from Saved reports. Each saved answer is independent; no revisions or chat transcripts are persisted.
7. To demonstrate lifecycle, start a fresh investigation and click Stop while it is active. A later follow-up can continue the open session. Closing the tab ends that session; reopening the app retains saved reports but no conversation.

## What to explain

- Copilot SDK/CLI owns the model/tool loop. Application code supplies domain instructions, read-only tools, evidence validation and cancellation.
- MCP reads SQL-backed mock CI/CD facts. The browser's REST reads do not replace the agent's MCP calls.
- The rounding patch, failed midpoint assertion and successful baseline support the finding. The cache warning also appears in the successful run.
- Reports describe evidence and recommend a check; the agent never applies or tests a fix.
- Follow-up memory lives only in the active SDK session. Saved reports are history, not persistent model memory.

## If a request fails

The UI displays the actual error. A validation failure can be addressed with an explicit follow-up asking for a corrected compact report using observed sources. There is no automatic retry or reconnect. Start fresh investigation if the SDK session has ended. A failed Save does not show Saved; retry only while the completed result remains available in the open session.

Only S01 is in the ordinary dataset. Negative-case demonstration, the full S01–S09 catalog and three-run scenario evaluations remain Stage 5 work. See [Stage 4 review](stage-4-review.md) for actual results and limits.
