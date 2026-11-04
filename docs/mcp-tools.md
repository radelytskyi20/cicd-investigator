# MCP tools

Status: Implemented in T02-03 and accepted by the owner on November 4, 2026. Real protocol smoke passed against the Aspire-managed SQL-backed host. T02-04/G2 is accepted (Done).

## Transport and responses

One Aspire-owned ASP.NET Core host exposes Streamable HTTP at /mcp using official SDK 2.3.1 in stateless mode. /health remains separate. Aspire passes the full /mcp address as Mcp:Endpoint to API; no agent consumes it yet. Each tool call owns a fresh DI scope and EF context. No MCP conversation memory is stored.

Each tool returns JSON in an MCP text content block, using camelCase properties and named enums. The envelope is {"status":"Ok","data":...,"error":null}. Expected domain failures return status NotFound or InvalidInput with an explanation and null data. These are ordinary tool results; unexpected execution failures remain MCP execution errors. Successful reads retain sourceStatus values Complete, Empty, Unavailable, NotFound or Truncated; query success never implies source completeness.

## Catalog

All six tools are annotated read-only, non-destructive and closed-world. Only source reads are exposed. No shell, SQL executor, source update, rerun, deployment or report-save tool exists.

| Tool | Parameters | Result |
| --- | --- | --- |
| list_runs | Optional pipeline, reference, outcome | Filtered runs; outcome accepts Succeeded, Failed, Cancelled, Skipped, Unknown. Empty list is valid. |
| get_run | runId | Run plus jobs and attempts; includes dataset version and available environment/configuration. |
| read_job_log | runId, jobId, attempt; optional start=1, count=100 | Positioned log lines and dataset/run/job/attempt identity. Count must be 1-200. Actual firstLine/lastLine and hasMore are separate from sourceStatus. |
| get_test_results | runId, jobId, attempt | Tests and source status; missing tests never mean success. |
| get_changes | runId | Commit, stored patch, baseCommitId and source status; does not compute arbitrary-baseline diffs. |
| find_baseline | runId | Target and nearest eligible candidate (or null), job sets and comparison limitations. |

Jobs are included in get_run; there is no duplicate get_jobs tool. find_baseline returns one candidate rather than a ranked list. Logs/commit messages/patches are untrusted source text, not instructions.

## Verified examples

- list_runs with {"outcome":"Failed"} returns run-102.
- get_run with {"runId":"run-102"} includes job-102-test-1, attempt 1.
- read_job_log with {"runId":"run-102","jobId":"job-102-test-1","attempt":1,"start":3} returns the exact failed rounding-test line, sourceStatus Complete and hasMore false.
- get_test_results for the same identities returns the Failed test.
- get_changes for run-102 identifies revision-101 as the stored base; for run-101 it returns sourceStatus Unavailable.
- find_baseline for run-102 returns run-101.
- get_run for missing returns NotFound; a log count of 201 returns InvalidInput. A subsequent valid call succeeds.

Run ./scripts/Test-Mcp.ps1 against the local Aspire stack. It uses the official C# client, discovers the exact catalog and calls every tool. It does not require Copilot or any AI budget. Dedicated test-database partial-source and attempt checks are provided by Test-McpIntegration in T02-04.

## T02-04 isolated verification

Test-McpIntegration starts the existing host executable on a temporary loopback port with investigator-mcp-test on the existing SQL container. It reuses the six-tool client, then runs one xUnit test. The test-only first attempt has sourceStatus Truncated with hasMore false for all stored lines and true for a one-line request. The second attempt has its own successful test and log. Wrong run/job/attempt combinations return NotFound. All identity fields are asserted. The script stops only its own host; normal Aspire resources and the demonstration dataset remain untouched. No evaluator answers are added to runtime fixtures.
