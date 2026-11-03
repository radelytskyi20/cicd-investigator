# MCP tools

Status: Planned. No MCP server, endpoint or tool has been implemented or protocol-tested.

## Proposed catalog

T02-03 proposes list_runs, get_run, get_jobs, read_job_log, get_test_results, get_changes and find_baselines. Streamable HTTP with one Aspire-owned server is proposed; final contracts will follow actual SDK compatibility checks.

## Planned tool documentation

For each implemented tool, record purpose, parameters, response examples, run/job/attempt/dataset identity, locators, range limits, completeness and errors. Distinguish empty, missing, unavailable and truncated data. Expose source facts without evaluator diagnoses.

## Planned verification and permissions

T02-04 will use a protocol client against SQL Server to discover and call every tool, including partial sources and invalid inputs. HTTP health checks alone do not verify MCP. Tools are read-only: no shell, SQL executor, rerun, Git, deployment or report saving.
