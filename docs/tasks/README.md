# Implementation task index

Version 1.2 | November 3, 2026 | 19 tasks across five stages

# Coding agent workflow

Read [decisions](../decisions.md), [requirements](../requirements.md) and the assigned task before implementation. In the target repository, read applicable AGENTS.md and README files, inspect Git status and verify dependencies. Implement only the assigned scope with the specified stack, run relevant checks, update affected English documentation and return actual results and limitations.

Use one task/Txx-xx branch and focused English commits. The coding agent sets Ready for review; the owner accepts Done and decides merge. Do not push, merge, rewrite history or start another task automatically. Do not mark an unverified check as passed.

T01-00 adapts the English v1.2 implementation package into this repository. Task files live directly in docs/tasks/; requirements and decisions use their normalized docs paths. No application task is implemented by importing its description.

Subsequent tasks read AGENTS.md, docs/decisions.md, docs/requirements.md and docs/tasks/Txx-xx.md. The task index tracks status and dependencies. Detailed commands go into docs/development.md only once verified; task completion notes record execution and outcome.

The target repository path is supplied when T01-00 is assigned. Do not initialize Git in an arbitrary chat directory. All repository-facing documentation and task reports are English; owner discussion can remain Russian.

Example assignment: Implement T01-00 from this package. Prepare the repository instructions and documentation only. Follow the stack, scope and acceptance checks, provide the review result and commit hash, and do not start T01-01 until this task has been accepted and that task is assigned.

## Status definitions

- Planned: not implemented; prerequisites and a separate assignment are required before work.
- In progress: the currently assigned task is being implemented.
- Ready for review: implementation is submitted with actual checks and explicit limitations; owner acceptance is pending.
- Done: accepted by the owner.
- Blocked: progress requires resolution of a recorded blocker.

G1 is accepted by the owner; G2-G5 are Planned. Dependencies form the listed sequence. Later-stage implementation requires a reviewed small-task breakdown and explicit owner approval.


## Stage 1 Repository preparation environment and Aspire startup

G1: Codex-ready repository and verified environment; AppHost starts SQL, API, MCP and SPA; EF connectivity passes and the foundation is committed.

- [T01-00 Prepare the repository for Codex](T01-00.md) | Done | Depends on: none
- [T01-01 Configure the development environment and pin versions](T01-01.md) | Done | Depends on: [T01-00](T01-00.md)
- [T01-02 Scaffold the solution and applications](T01-02.md) | Done | Depends on: [T01-01](T01-01.md)
- [T01-03 Orchestrate the local stack with Aspire](T01-03.md) | Done | Depends on: [T01-02](T01-02.md)

## Stage 2 Domain data and MCP server

G2: MCP protocol tests pass independently of the model against SQL Server.

- [T02-01 Create the EF model and initial mock data](T02-01.md) | Done | Depends on: [T01-03](T01-03.md)
- [T02-02 Implement domain queries and baseline selection](T02-02.md) | Planned | Depends on: [T02-01](T02-01.md)
- [T02-03 Expose the MCP server tools](T02-03.md) | Planned | Depends on: [T02-02](T02-02.md)
- [T02-04 Verify MCP independently of the model](T02-04.md) | Planned | Depends on: [T02-03](T02-03.md)

## Stage 3 Copilot SDK agent and session memory

G3: real SDK uses MCP tools, runs the agent loop, demonstrates session memory and cancels correctly.

- [T03-01 Validate Copilot SDK with the MCP server](T03-01.md) | Planned | Depends on: [T02-04](T02-04.md)
- [T03-02 Implement connection-scoped agent sessions](T03-02.md) | Planned | Depends on: [T03-01](T03-01.md)
- [T03-03 Implement investigation behavior and report validation](T03-03.md) | Planned | Depends on: [T03-02](T03-02.md)
- [T03-04 Verify agent loop memory and cancellation](T03-04.md) | Planned | Depends on: [T03-03](T03-03.md)

## Stage 4 API and minimal user interface

G4: the full UI path works; tab closure stops analysis and only completed reports are saved.

- [T04-01 Expose read APIs and persist completed reports](T04-01.md) | Planned | Depends on: [T03-04](T03-04.md)
- [T04-02 Build the run catalog and details UI](T04-02.md) | Planned | Depends on: [T04-01](T04-01.md)
- [T04-03 Build investigation chat and report history](T04-03.md) | Planned | Depends on: [T04-02](T04-02.md)
- [T04-04 Verify the integrated UI API and agent](T04-04.md) | Planned | Depends on: [T04-03](T04-03.md)

## Stage 5 Mock data evaluation and finalization

G5: primary and one negative case are evaluated three times each; fresh checkout and demo are reproducible.

- [T05-01 Finalize the compact S01 to S09 mock dataset](T05-01.md) | Planned | Depends on: [T04-04](T04-04.md)
- [T05-02 Evaluate the agent and prepare the memory demonstration](T05-02.md) | Planned | Depends on: [T05-01](T05-01.md)
- [T05-03 Finalize reproducible startup and MVP documentation](T05-03.md) | Planned | Depends on: [T05-02](T05-02.md)

## Approved foundation subtasks (November 4, 2026)

The owner clarified on November 4, 2026 that only these five foundation slices are approved for execution, preserving the 19 parent task IDs. The broader execution authorization is superseded. Stage 2-5 require a small-task breakdown and explicit owner approval before implementation. Deferred Stage 2-3 work was withdrawn; useful observations remain in [archived research](../research/deferred-stage-2-3.md). All Foundation tasks were accepted on November 4, 2026.

- [T01-01-01 Verify prerequisites and pin compatible versions](T01-01-01.md) | Done | Blocked by: T01-00 (Done)
- [T01-02-01 Build and launch the application shells](T01-02-01.md) | Done | Blocked by: T01-01-01
- [T01-03-01 Run SQL Server and verify EF connectivity through Aspire](T01-03-01.md) | Done | Blocked by: T01-02-01
- [T01-03-02 Launch the complete local stack from AppHost](T01-03-02.md) | Done | Blocked by: T01-03-01
- [T01-03-03 Verify restart and document reproducible startup](T01-03-03.md) | Done | Blocked by: T01-03-02


## Approved T02-01 subtasks (November 4, 2026)

Implemented and accepted by the owner on November 4, 2026. Later tasks still require separate approval.

- [T02-01-01 Persist a minimal failed run and its successful predecessor](T02-01-01.md) | Done | Blocked by: T01-03 (Done)
- [T02-01-02 Persist traceable log and test evidence](T02-01-02.md) | Done | Blocked by: T02-01-01
- [T02-01-03 Complete the initial S01 investigation dataset](T02-01-03.md) | Done | Blocked by: T02-01-02
- [T02-01-04 Verify repeatable initialization and restart](T02-01-04.md) | Done | Blocked by: T02-01-03
