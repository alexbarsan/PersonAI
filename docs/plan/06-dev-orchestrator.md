# Dev Orchestrator

`decision-record.md` is authoritative. This file defines the agent workflow for executing slices.

## Master Orchestrator Prompt

Use this prompt when starting a build session:

```text
You are building DreamLens and PersonaKit in this repository.

Read docs/plan/decision-record.md first. It is the single source of truth. If any document, prompt, implementation idea, or generated code conflicts with it, stop and update the decision record first or ask the human for a decision.

Execute slices in strict order S0 through S21. For each slice:
1. Read the relevant slice prompt.
2. Write the listed tests first.
3. Run the tests and confirm they fail for the expected reason.
4. Implement the minimum production code to pass.
5. Refactor while keeping tests green.
6. Run the full relevant suite.
7. Update SLICE-STATUS.md.
8. Commit once with a conventional commit: feat(Sx): short description.

Do not skip slices. Do not proceed on a red suite. Do not send PII or sensitive content to AI providers in tests or logs. Keep DreamLens wellness/entertainment only and never medical, psychological, or diagnostic.
```

If the plan files have not yet moved to `docs/plan/`, read them from the repository root until S0 normalizes the layout.

## Per-Slice Workflow

1. Context pass: read `decision-record.md`, this file, and the current slice prompt.
2. Scope pass: list files expected to change.
3. Red pass: create tests first and run the narrow suite.
4. Green pass: implement the minimum production code.
5. Refactor pass: clean naming, duplication, and boundaries.
6. Full verification: run the full suite relevant to current repo state.
7. Status pass: update `SLICE-STATUS.md`.
8. Commit pass: commit exactly once if the user has authorized slice execution.

## Status Format

`SLICE-STATUS.md` tracks:

- slice id
- status: `Not started`, `In progress`, `Blocked`, `Done`
- date completed
- commit hash
- verification command
- notes

## Human 5-Minute Audit

After each slice, the human should check:

- Does the implementation match the slice scope?
- Were tests written first and are they meaningful?
- Did the full relevant suite pass?
- Did the slice avoid unrelated refactors?
- Did any decision conflict require updating `decision-record.md`?
- Are privacy and logging invariants preserved?
- Is `SLICE-STATUS.md` updated?
- Is there exactly one commit for the slice?

## Stuck Or Red-Suite Recovery

If the suite is red after implementation:

1. Identify whether the failure is test bug, production bug, environment issue, or decision conflict.
2. Fix test bugs only when the original expected behavior was wrong or incomplete.
3. Fix production bugs without widening scope.
4. If environment blocks verification, document the command, failure, and blocker in `SLICE-STATUS.md`.
5. If a decision conflict appears, update `decision-record.md` first.
6. Do not continue to the next slice until the issue is resolved or explicitly accepted by the human.

## ADR Update Prompt

Use when a decision needs to change:

```text
We found a conflict with decision-record.md:
- Current decision:
- Proposed change:
- Reason:
- Affected docs:
- Affected code:
- Migration or cleanup required:

Update decision-record.md first, then update dependent docs and implementation.
```

## Commit Rules

Format:

```text
feat(S0): initialize monorepo foundation
feat(S1): add walking skeleton API
fix(S4): correct profile consent encryption
docs(plan): update decision record for retention policy
```

Slice commits use `feat(Sx): ...` unless the slice is explicitly documentation-only or a correction to a previous committed slice.
