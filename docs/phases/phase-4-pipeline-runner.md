# Phase 4 — Pipeline Runner

## Goal

Implement the orchestration loop. Walk the pipeline in order, call `IExpertRunner` per step, pass context forward, write step outputs to `runs/`. No MAF dependency — only depends on `IExpertRunner`.

## Completion condition

All unit tests pass using a stub `IExpertRunner`. Pipeline correctly sequences steps, passes prior output as context, and writes output files — with no LLM involved.

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Define `IExpertRunner` interface (`RunAsync(ExpertDefinition, string context, CancellationToken)`) | Not Started |
| 2 | Define `PipelineRunOptions` (`MissionName`, `InputText`, `OutputDirectory`) | Not Started |
| 3 | Implement `PipelineRunner` — walks pipeline steps in order | Not Started |
| 4 | Implement context passing — output of step N becomes input context of step N+1 | Not Started |
| 5 | Implement output writer — writes each step to `runs/<mission>/<NN>-<ExpertName>.md` | Not Started |
| 6 | Implement `final.md` — writes the last step output as `runs/<mission>/final.md` | Not Started |
| 7 | Implement step numbering — zero-padded prefix (01, 02, ...) | Not Started |
| 8 | Implement `StubExpertRunner` in test project — returns canned string for unit tests | Not Started |
| 9 | Unit test: single-step pipeline calls runner once and writes output | Not Started |
| 10 | Unit test: multi-step pipeline passes correct context to each step | Not Started |
| 11 | Unit test: output files are created with correct names and numbering | Not Started |
| 12 | Unit test: `final.md` matches last step output | Not Started |
| 13 | Unit test: cancellation token is respected | Not Started |
