# Evaluation

Status: Planned. No application, model evaluation or integration result exists yet.

## Proposed cases

S01 is the proposed primary demonstration; S04 is the proposed single negative example. These remain proposals. S01–S09 semantics are defined in [requirements](requirements.md).

## Planned execution

T05-02 requires at least three independent real evaluations per selected case. Begin each evaluation with a fresh SDK session; test follow-up memory within that session and isolation in a new one. Record versions, evidence references, duration, tool-call count, outcomes and limitations without runtime transcript dumps.

## Planned assessment

Check facts, source ownership, meaningful tool use, competing hypotheses, confidence and useful recommendations. Unknown cause is valid when sources are insufficient. Fabricated evidence and unauthorized actions are defects. Separate deterministic tests, protocol checks, fake adapter checks and real AI results; authentication failures are not passes.

## Evaluator isolation

Future answer keys belong outside agent-visible sources, prompts and runtime working directories. This developer-facing document and the scenario descriptions are not runtime Investigator instructions. No evaluation runs have been performed.
