# Phase 4 — Pipeline Runner

## Goal

Implement the orchestration loop. Walk the pipeline in order, call `IExpertRunner` per step, pass context forward, write step outputs to `runs/`. No MAF dependency — only depends on `IExpertRunner`.

## Completion condition

All unit tests pass using a stub `IExpertRunner`. Pipeline correctly sequences steps, passes prior output as context, and writes output files — with no LLM involved.

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Define `IExpertRunner` interface (`RunAsync(ExpertDefinition, string context, CancellationToken)`) | Done |
| 2 | Define `PipelineRunOptions` (`MissionName`, `InputText`, `OutputDirectory`) | Done |
| 3 | Implement `PipelineRunner` — walks pipeline steps in order | Done |
| 4 | Implement context passing — output of step N becomes input context of step N+1 | Done |
| 5 | Implement output writer — writes each step to `runs/<mission>/<NN>-<ExpertName>.md` | Done |
| 6 | Implement `final.md` — writes the last step output as `runs/<mission>/final.md` | Done |
| 7 | Implement step numbering — zero-padded prefix (01, 02, ...) | Done |
| 8 | Implement `StubExpertRunner` in test project — returns canned string for unit tests | Done |
| 9 | Unit test: single-step pipeline calls runner once and writes output | Done |
| 10 | Unit test: multi-step pipeline passes correct context to each step | Done |
| 11 | Unit test: output files are created with correct names and numbering | Done |
| 12 | Unit test: `final.md` matches last step output | Done |
| 13 | Unit test: cancellation token is respected | Done |

## Result

5/5 pipeline runner tests passing. 21/21 total tests passing.

## Notes

- `PipelineRunner` flattens composite expert declarations recursively before executing — a mission step that is itself an `expert` declaration gets expanded into its constituent leaf experts
- Circular reference detection is in place during flattening
- `StubExpertRunner` lives in the test project and captures all calls for assertion — useful for verifying context chaining
