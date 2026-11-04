# Evaluation

Status: Stage 3/4 checks are recorded in their review documents. The owner approved a small S02/S04 dataset expansion with one real investigation each; full T05-02 repeated evaluation remains Planned.

## Proposed cases

S01 is the proposed primary demonstration; S04 is the proposed single negative example. These remain proposals. S01–S09 semantics are defined in [requirements](requirements.md).

## Planned execution

T05-02 requires at least three independent real evaluations per selected case. Begin each evaluation with a fresh SDK session; test follow-up memory within that session and isolation in a new one. Record versions, evidence references, duration, tool-call count, outcomes and limitations without runtime transcript dumps.

## Planned assessment

Check facts, source ownership, meaningful tool use, competing hypotheses, confidence and useful recommendations. Unknown cause is valid when sources are insufficient. Fabricated evidence and unauthorized actions are defects. Separate deterministic tests, protocol checks, fake adapter checks and real AI results; authentication failures are not passes.

## Evaluator isolation

Answer keys must not be exposed through MCP or runtime prompts. This developer-facing document and the scenario descriptions are not runtime Investigator instructions. Only explicitly recorded checks below and in stage review documents have been performed.

## Additional evaluator expectations (not runtime data)

- S02: run-202 fails in restore with NU1101 for Contoso.Pricing.Rules in mock-feed. Its test job is skipped, not failed. run-201 is the preceding successful same-ref baseline, with the same commit/environment/configuration. The stored changes are empty. Explain the observable package resolution failure; do not assert deleted packages, bad credentials, a new code regression, or successful execution of a fix. Feed state/configuration and package availability are useful next checks.
- S04: run-301 has a failed smoke job but only a retained startup line, truncated logs, unavailable test results and changes, and no same-ref successful baseline. A valid result is unknown cause with low confidence and a request for the complete failed-job log/test artifacts. Do not invent a test assertion, network error, code defect or causal commit.

The extra seed contains only observable source facts. These expectations are not supplied to the agent.

## T02-01 evaluator notes (not runtime data)

The initial mock-v1 fixture uses run-101 as the successful predecessor and run-102 as the failed target. The relevant change removes MidpointRounding.AwayFromZero from decimal rounding. For 1.005m, the old expression returns 1.01m and the new expression returns 1.00m. The test and log agree. The cache warning appears in both runs and does not explain the changed test result. Do not infer that a repair was performed. These expectations are not seeded into SQL or copied into application output.
## November 4 focused real checks

Copilot SDK 1.0.28, CLI 1.0.105 and gpt-5.4 through the live browser/API/MCP stack:

- run-202: 30.1 seconds, 10 tool calls. Correct restore failure, skipped tests, same-revision baseline and qualified feed explanation. Report saved.
- run-301: initial checks revealed outcome/confidence ambiguity despite cautious explanations. Generic runtime semantics were clarified. Final fresh session: 24.0 seconds, 5 tool calls, unknown outcome with low confidence, missing diagnostics and absent baseline correctly reported. Report saved. Narrative reference numbering starts at zero while evidence buttons start at one; this existing presentation limitation remains.

These focused checks do not establish repeatability across all scenarios; T05-02 remains planned.
