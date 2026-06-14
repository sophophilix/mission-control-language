# Phase 6 — CLI

## Goal

Wire everything together behind `fml run`, `fml validate`, and `fml list experts`. Thin entry point — no business logic in the CLI layer.

## Completion condition

End-to-end run of the `build-operator` example produces correctly named output files in `runs/build-operator/`.

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Set up `System.CommandLine` root command and subcommands | Not Started |
| 2 | Implement `fml run <mission.fml> --input <input.md>` — wires parser → loader → runner | Not Started |
| 3 | Implement `fml validate <mission.fml>` — runs parser and expert loader validation only | Not Started |
| 4 | Implement `fml list experts` — lists all `.md` files in `experts/` with their names | Not Started |
| 5 | Wire DI container — register `MafExpertRunner` as `IExpertRunner` | Not Started |
| 6 | Implement error output — parse errors and validation errors print cleanly to stderr | Not Started |
| 7 | Create `examples/build-operator/` directory structure | Not Started |
| 8 | Create `examples/build-operator/mission.fml` | Not Started |
| 9 | Create `examples/build-operator/input.md` | Not Started |
| 10 | Create `examples/build-operator/experts/KubernetesArchitect.md` | Not Started |
| 11 | Create `examples/build-operator/experts/SecurityArchitect.md` | Not Started |
| 12 | Create `examples/build-operator/experts/PrincipalReviewer.md` | Not Started |
| 13 | End-to-end test: `fml run` produces output files in correct structure | Not Started |
