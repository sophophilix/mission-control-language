# Mission Control Language (MCL)

**Expert** = reusable intelligence  
**Mission** = reasoning pattern  
**Agent**   = endpoint / runtime facade

That is the architecture. Everything else follows from it.

---

## What MCL is

MCL is a language for expressing expertise and reasoning patterns.

Not a workflow engine. Not a prompt template system. Not an agent framework.

The core insight is that intelligence is composable — and that composition should be expressible as language, not code.

---

## The three concepts

### Expert

An Expert is a reusable intelligence package.

Experts encapsulate knowledge, methodology, review criteria, and domain expertise. They are designed to be portable and distributable — first-class artifacts, not inline prompt strings.

```fsharp
expert KubernetesArchitect =
    from "ghcr.io/katasec/forge-kubernetes-architect"
    version "0.1.0"
```

Experts can be composed from other experts:

```fsharp
expert KubernetesArchitect =
    RequirementsAnalyst
    |> PlatformArchitect
    |> ReliabilityArchitect
```

An Expert answers: **Who performs this work?**

---

### Mission

A Mission is a reasoning pattern.

A mission describes how expertise flows — which experts collaborate, in what order, and how their outputs accumulate.

```fsharp
mission BuildOperatorDesign(goal, persona) =
    KubernetesArchitect
    |> SecurityArchitect
    |> PrincipalReviewer with { style = "terse ADR" }
```

The mental model is expertise flow, not function composition:

```
KubernetesArchitect
        ↓
SecurityArchitect
        ↓
PrincipalReviewer
```

Each stage receives the accumulated context and all prior outputs, then applies its domain expertise before passing forward.

A Mission answers: **How should expertise collaborate?**

---

### Agent

An Agent is a runtime endpoint.

An agent exposes one or more missions behind a conversational interface — VS Code, Open WebUI, Claude Code, or any OpenAI-compatible client.

```fsharp
agent BuildOperator =
    uses BuildOperatorDesign
```

The user talks to the agent. The agent runs the mission. The mission coordinates the experts.

An Agent answers: **How does a user consume this capability?**

---

## OCI-distributed experts

One of the most significant aspects of MCL is how experts are distributed.

Experts are not prompts. They are packaged expertise — versioned, addressable, and shareable across projects and teams.

MCL uses OCI (Open Container Initiative) registries as the distribution mechanism. The same infrastructure that distributes container images and Helm charts now distributes expertise:

```fsharp
expert SecurityArchitect =
    from "ghcr.io/katasec/forge-security-architect"
    version "0.1.0"
```

`forge init` resolves expert references and caches them in `~/.forge/experts/`. The `mcl.lock` file records exactly what was resolved, mirroring Terraform's plan/apply discipline.

```bash
forge init                       # pull experts from OCI, generate mcl.lock
forge run                        # run with resolved experts
forge clean                      # purge the local cache
forge clean --registry ghcr.io   # purge one registry
forge login ghcr.io --token <pat> # authenticate to a private registry
```

This turns expertise into a supply chain — versioned, auditable, and independently evolved.

---

## The language boundary

MCL is intentionally not a general-purpose programming language.

The language expresses reasoning, not computation. `|>` is an expertise flow operator. There are no loops over data, no branching on values, no string manipulation.

**Right direction:**

```fsharp
Architect |> Reviewer |> Judge
```

**Wrong direction:**

```fsharp
for  while  if  match  map  filter
```

When a mission needs to retry until quality passes, that is a reasoning pattern — expressed as a loop cap on the mission, not imperative control flow:

```fsharp
mission RefinedPitch(product) =
    PitchDrafter
    |> PitchCritic
    |> PitchJudge
    loop 3
```

The language has one job: express how expertise flows.

---

## Mission Control

MCL is the language. Mission Control is the operating environment around it.

Mission Control addresses a deeper problem: **context entropy**. Large monolithic context degrades LLM effectiveness. As a session accumulates work, precision degrades.

Mission Control's approach is hub/spoke knowledge organisation:

```
plan.md   ←  hub (index of intent)
    ├── phase-11.md
    ├── phase-12.md
    └── architecture.md
```

The plan acts as an index. Individual tasks and decisions live in focused spoke documents. This keeps context targeted and continuity intact across long-running work.

Longer term, Mission Control introduces the Session Continuity Protocol (SCP) — a protocol for maintaining plans, decisions, and context across sessions without blowup. The hub/spoke pattern is already in use throughout this project.

---

## Quick start

```bash
export MCL_API_KEY=sk-...

forge init    # resolve experts, generate mcl.lock
forge run     # run the mission, output to stdout
```

---

## Writing a mission

A mission file is self-contained. It declares where experts come from, binds input values, and describes the reasoning pattern.

```fsharp
// Provider
let provider = env("MCL_PROVIDER", "openai")
let apiKey   = env("MCL_API_KEY")
let model    = env("MCL_MODEL", "gpt-4o-mini")

// Inputs
let goal    = "Design a production-grade K8s build operator"
let persona = "Principal SRE, Tekton specialist"

// Experts — pulled from OCI on forge init
expert KubernetesArchitect =
    from "ghcr.io/katasec/forge-kubernetes-architect"
    version "0.1.0"

expert SecurityArchitect =
    from "ghcr.io/katasec/forge-security-architect"
    version "0.1.0"

expert PrincipalReviewer =
    from "ghcr.io/katasec/forge-principal-reviewer"
    version "0.1.0"

// Reasoning pattern
mission BuildOperatorDesign(goal, persona) =
    KubernetesArchitect
    |> SecurityArchitect
    |> PrincipalReviewer with { style = "terse ADR" }
```

---

## Writing an expert

Each expert is a markdown file with a YAML frontmatter header and a system prompt.
`{{key}}` placeholders are interpolated from the context bag before the step runs.

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

Scaffold a new expert:

```bash
forge expert init SecurityArchitect
```

Local experts live alongside the mission file. OCI experts are cached globally under
`~/.forge/experts/` and shared across all projects.

```
missions/build-operator/
  mission.mcl
  mcl.lock
  experts/
    SecurityArchitect/
      expert.md
```

---

## CLI

```bash
forge init                                     # resolve experts, write mcl.lock
forge validate                                 # lint the mission file (syntax only)
forge run                                      # run the mission
forge run --steps                              # stream each expert's output live
forge run --var goal="Redesign for ARM64"      # override a let binding at runtime
forge clean                                    # purge ~/.forge/experts
forge clean --registry ghcr.io                # purge one registry
forge login ghcr.io --token <pat>             # save registry credentials
forge expert init SecurityArchitect           # scaffold a new expert
forge list experts                            # list local experts
```

`forge run` requires an `mcl.lock` — `forge init` is not optional.

---

## Variables

`let` bindings seed the context bag. Every expert in the pipeline can read any binding
via `{{key}}` in its system prompt.

```fsharp
let goal   = "Design a build operator"
let apiKey = env("MCL_API_KEY")
let model  = env("MCL_MODEL", "gpt-4o-mini")
```

### Provider configuration

Four bindings are reserved for LLM provider configuration:

```fsharp
let provider = env("MCL_PROVIDER", "openai")     // openai (azure and others planned)
let apiKey   = env("MCL_API_KEY")                // required — no default
let model    = env("MCL_MODEL", "gpt-4o-mini")
let endpoint = env("MCL_ENDPOINT", "")           // required for Azure
```

`with` overrides context for a single step only:

```fsharp
|> PrincipalReviewer with { style = "terse ADR", audience = "C-suite" }
```

Variable resolution order (lowest → highest precedence):

1. `let` binding
2. `with { }` clause on the step
3. `--var key=value` CLI flag

---

## Pass / fail

Every step passes by default — like a shell command exiting with code 0.
A step signals failure by returning a structured fail status in its output.

If any step fails, the pipeline stops immediately. Expert authors declare failure
conditions in plain prose — the runtime handles the structured contract:

```markdown
You are the final judge. If the pitch is unclear, too long, or contains jargon —
declare failure and state which criterion was missed.
```

---

## Looping until quality passes

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
| `{{attempt}}` | Current attempt number, 1-based. |
| `{{max_loops}}` | Declared loop cap. |

Experts can use these to adjust behaviour across retries:

```markdown
This is attempt {{attempt}} of {{max_loops}}.
If this is the final attempt, be especially strict — there are no more chances to improve.
```

---

## Reserved context variables

| Variable | Value |
|----------|-------|
| `{{output}}` | The previous step's text output. Empty string on the first step. |
| `{{attempt}}` | Current loop iteration, 1-based. Always `1` for missions without `loop`. |
| `{{max_loops}}` | Declared loop cap. Always `1` for missions without `loop`. |

---

## Output routing

By default the mission result goes to stdout — pipeable like any CLI tool:

```bash
forge run                        # stdout
forge run > report.md            # redirect
forge run | pbcopy               # pipe
```

Declare the destination in the mission file to make it explicit:

```fsharp
output(BuildOperatorDesign)                 # stdout (default)
output(BuildOperatorDesign, "./report.md")  # write to file
```

Status messages always go to stderr and never pollute the output stream.

---

> Why MCL exists: [docs/why.md](docs/why.md)
