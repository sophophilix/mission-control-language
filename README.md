# Forge Mission Script (FMS)

A minimal scripting language for composing LLM experts into reliable reasoning pipelines.

> **Why it exists:** see [docs/why.md](docs/why.md)

---

## Quick start

```bash
export OPENAI_API_KEY=sk-...

fms init       # resolve experts, generate fms.lock
fms run        # run the mission, output to stdout
```

---

## Writing a mission

A mission file is self-contained. It declares where experts come from, binds any input values,
and describes the pipeline.

```fsharp
use "./experts"

let goal    = "Design a production-grade K8s build operator"
let persona = "Principal SRE, Tekton specialist"

mission BuildOperatorDesign(goal, persona) =
    KubernetesArchitect
    |> SecurityArchitect
    |> PrincipalReviewer with { style = "terse ADR" }

output(BuildOperatorDesign)
```

The `|>` operator means progressive refinement — each expert receives the previous expert's
output and improves or constrains it. This is not function composition; it is sequential
reasoning with accumulating context.

---

## CLI

```bash
# Resolve expert sources and write fms.lock
fms init

# Check all experts resolve, pipeline is well-formed, lock file is current
fms validate

# Run the mission — output to stdout
fms run

# Stream each expert's progress to stderr as the pipeline runs
fms run --steps

# Override any let binding at run time
fms run --var goal="Redesign for ARM64"

# Scaffold a new expert directory
fms expert init SecurityArchitect

# List experts in the current directory
fms list experts
```

`fms run` requires an `fms.lock` — init is not optional. This mirrors Terraform's
`plan` / `apply` discipline: resolve first, then run.

---

## Output routing

By default the mission result goes to stdout — pipeable like any CLI tool:

```bash
fms run                          # stdout
fms run > report.md              # redirect
fms run | pbcopy                 # pipe
```

Declare the destination in the mission file to make it explicit:

```fsharp
output(BuildOperatorDesign)                  # stdout (default)
output(BuildOperatorDesign, "./report.md")   # write to file
```

Status messages (`Running mission...`, step progress) always go to stderr and never
pollute the output stream.

---

## Experts

Each expert is a markdown file with a YAML frontmatter header and a system prompt.
`{{key}}` placeholders are interpolated from the context bag before each step runs.

```markdown
---
name: KubernetesArchitect
input: Task description
output: Kubernetes architecture design
---

You are a senior Kubernetes architect.

Goal: {{goal}}
Perspective: {{persona}}

Produce a concrete architecture covering CRD design, controller structure,
RBAC, and operational concerns.
```

Experts live in a directory declared by `use`:

```
missions/build-operator/
  mission.fms
  fms.lock
  experts/
    KubernetesArchitect/
      expert.md
    SecurityArchitect/
      expert.md
    PrincipalReviewer/
      expert.md
```

Experts can be composed from other experts:

```fsharp
expert KubernetesArchitect =
    RequirementsAnalyst
    |> PlatformArchitect
    |> ReliabilityArchitect
```

---

## Variables

`let` bindings seed the context bag. Every expert in the pipeline can read any binding
via `{{key}}` in its system prompt.

```fsharp
let goal   = "Design a build operator"
let apiKey = env("OPENAI_API_KEY")              // from environment
let model  = env("FML_MODEL", "gpt-4o-mini")   // with default
```

`with` overrides context for a single step only:

```fsharp
mission BuildOperatorDesign(goal, persona) =
    KubernetesArchitect
    |> PrincipalReviewer with { style = "terse ADR", audience = "C-suite" }
```

Variable resolution order (lowest → highest precedence):

1. `let` binding
2. `with { }` clause on a step
3. `--var key=value` CLI flag

---

## Pass / fail

Every step in a pipeline passes by default — like a bash command exiting with code 0.
A step explicitly signals failure by returning a structured fail status in its output.

If any step fails, the pipeline stops immediately and the mission fails with that step's
reason. There is no silent continuation past a failure.

Expert authors declare failure conditions in plain prose in their system prompt:

```markdown
# PitchJudge/expert.md

You are the final judge. If the pitch is unclear, too long, or contains jargon —
declare failure and state which criterion was missed.
```

The runtime handles the structured contract. Expert authors write prose, not JSON.

---

## Looping until quality passes

A mission can declare a loop cap — the maximum number of times the full pipeline will
retry until all steps pass:

```fsharp
mission RefinedPitch(product) =
    PitchDrafter
    |> PitchCritic
    |> PitchReviser
    |> PitchJudge
    loop 3
```

The runtime retries the full pipeline on failure, up to the declared limit. Two reserved
variables are available to every expert in every attempt:

| Variable | Value |
|----------|-------|
| `{{attempt}}` | Current attempt number, 1-based. Always `1` for missions without `loop`. |
| `{{max_loops}}` | Declared loop cap. Always `1` for missions without `loop`. |

Experts can use these to adjust behaviour across retries:

```markdown
This is attempt {{attempt}} of {{max_loops}}.
If this is the final attempt, be especially strict — there are no more chances to improve.
```

---

## Reserved context variables

These are injected by the runtime and available to every expert. They cannot be overridden.

| Variable | Value |
|----------|-------|
| `{{output}}` | The previous step's text output. Empty string on the first step. |
| `{{attempt}}` | Current loop iteration, 1-based. |
| `{{max_loops}}` | Declared loop cap. |

---

## Repository structure

```
forge-mission-language/
  src/
    ForgeMission.Core/    # parser (ANTLR4), AST, pipeline runner, expert resolution
    ForgeMission.Cli/     # CLI — fms init / run / validate / list / expert
    ForgeMission.Tests/   # unit and integration tests
  missions/
    build-operator/       # production K8s operator design example
    elevator-pitch/       # single-expert pitch example
    elevator-pitch-refined/  # Generator → Critic → Reviser → Judge example
  docs/
    plan.md               # implementation plan — all phases
    why.md                # origin, methodology, thesis
    design/               # language design, architecture docs
    phases/               # per-phase specs
```

---

## Status

Working prototype. Phases 1–10 complete.

| Phase | Description | Status |
|-------|-------------|--------|
| 1–10 | Core runtime, parser, CLI, expert resolution, variables | Done |
| 12 | Structured step envelope — pass/fail per step | In development |
| 14 | `loop N` — retry pipeline until all steps pass | In development |
| 15 | Token streaming — live output as experts generate | In development |
| 16 | FMS rename — binary and extension surface rename | Pending |

See [`docs/plan.md`](docs/plan.md) for the full plan.

---

> **Note:** Pass/fail, `loop N`, and token streaming are currently in development.
> The concepts described in this README reflect the intended design. The CLI, pipeline runner,
> and expert composition are fully working today.
