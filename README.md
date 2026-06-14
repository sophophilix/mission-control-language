# Forge Mission Language (FML)

A minimal language for expressing how a problem should be reasoned about — as a composition of experts.

---

## What it is

Forge Mission Language lets you define a **mission** — a problem or desired outcome — and express how it should be reasoned about as a pipeline of **experts**.

```fsharp
mission BuildOperator =
    KubernetesArchitect
    |> SecurityArchitect
    |> PrincipalReviewer
```

This is not an execution plan. It is a reasoning structure. Each expert applies a lens, refines the previous output, and passes a better-constrained result to the next. The `|>` operator is progressive refinement, not function composition.

The language has three primitives: `mission`, `expert`, and `|>`. That is intentional.

---

## Where it came from

FML emerged from six months of real-world LLM usage across meaningfully different problem domains: production debugging on custom applications, Envoy proxy configuration, Kubernetes and Helm operations, and software development in Go and C#.

In every case, getting reliable output required the same manual work: decompose the problem, identify the relevant reasoning lenses, sequence them deliberately, and structure the handoff between them. That process was always implicit — buried in ad-hoc prompts, markdown files, and trial and error.

FML is the codification of that process. It makes the reasoning structure explicit, named, composable, and reviewable.

---

## The broader methodology

FML does not stand alone. It is one layer in a deliberate engineering approach to LLM-driven work:

1. **Design first** — never execute cold. Iterate on design until it is solid before any implementation begins.
2. **Phase decomposition** — break the design into agreed phases. Each phase is a meaningful, bounded unit of work.
3. **Atomic task generation** — per phase, generate tasks in sequential dependency order so each can be executed and tested before the next begins.
4. **Narrow execution** — by the time an agent executes, the work is so well-prescribed that there is little room for drift. The design thinking is already done.
5. **Oversight** — an architect agent reviews the work of the executing agent, catches omissions, and enforces quality gates including testing.
6. **Session continuity** — agent performance degrades as context fills. Sessions are treated as bounded units with structured handoffs, so a fresh agent picks up exactly where the last left off — at full capacity, with full context of what is done and what remains.

FML addresses layer one and three of this stack: expressing the reasoning structure of a design, and giving executing agents a clear, reviewable definition of how to approach a problem.

Without an explicit reasoning structure, agents work from vague instructions and produce inconsistent output. With one, the work is prescriptive enough to execute reliably and narrow enough to test.

---

## Core thesis

Large language models perform best when reasoning is constrained through deliberate decomposition and the application of expertise.

A single general-purpose prompt asks the model to architect, review, challenge, and conclude simultaneously. Expert composition asks each lens to do one thing well, in sequence, with the previous output as input.

**A mission is a reasoning structure, not an execution plan.**

---

## Why explicit reasoning structures matter

Most AI tooling expresses reasoning through prompts, markdown instructions, YAML, or agent configuration. This works at small scale, but the result is:

- **ambiguous** — intent is buried in prose, not structure
- **not composable** — reasoning patterns cannot be named, reused, or shared
- **not reviewable** — a human or oversight agent cannot inspect the reasoning approach, only the output
- **not handoff-friendly** — a fresh agent session cannot reconstruct how a problem was being reasoned about
- **execution-focused** — tool calls, retries, model selection get more attention than the reasoning itself

FML addresses all of these by making the reasoning structure a first-class artifact — something that can be written, reviewed, versioned, and handed to any agent session.

---

## Syntax

The language has three primitives:

| Primitive | Meaning |
|-----------|---------|
| `mission` | A problem or desired outcome |
| `expert`  | A reusable reasoning capability |
| `\|>`      | Progressive refinement / expert composition |

Identifiers are PascalCase. Keywords are lowercase. This is enforced by the grammar, not convention — experts are proper nouns representing roles, and the visual distinction from keywords matters when reading a pipeline.

### Defining a mission

```fsharp
mission BuildOperator =
    KubernetesArchitect
    |> SecurityArchitect
    |> PrincipalReviewer
```

### Defining a composed expert

Experts can be composed from other experts, giving the language recursive decomposition:

```fsharp
expert KubernetesArchitect =
    RequirementsAnalyst
    |> PlatformArchitect
    |> ReliabilityArchitect
```

### Expert definitions (markdown-backed)

Each expert is backed by a markdown file describing its reasoning role, inputs, and outputs:

```markdown
---
name: KubernetesArchitect
input: MissionBrief
output: ArchitectureProposal
---

You are a Kubernetes platform architect.

Your job is to:
- understand the mission
- propose a practical architecture
- identify tradeoffs
- explain operational risks
- produce a clear architecture proposal
```

---

## Thinking models FML can express

The same language expresses many common reasoning structures through expert composition.

### Progressive refinement

```fsharp
mission BuildOperator =
    KubernetesArchitect
    |> SecurityArchitect
    |> PrincipalReviewer
```

Each expert improves or constrains the previous output.

### Hierarchical decomposition

```fsharp
expert KubernetesArchitect =
    RequirementsAnalyst
    |> PlatformArchitect
    |> ReliabilityArchitect
```

A high-level expert is decomposed into smaller, more focused experts.

### Separation of concerns

```fsharp
mission DesignPlatform =
    PlatformArchitect
    |> SecurityReviewer
    |> CostReviewer
    |> ReliabilityReviewer
```

Each expert applies a distinct concern to the same problem.

### Scientific method

```fsharp
mission ValidateIdea =
    HypothesisBuilder
    |> ExperimentDesigner
    |> EvidenceReviewer
    |> ConclusionWriter
```

### OODA loop

```fsharp
mission RespondToIncident =
    Observer
    |> Orienter
    |> DecisionMaker
    |> Remediator
```

### Adversarial review

```fsharp
mission ReviewArchitecture =
    Architect
    |> Skeptic
    |> RiskReviewer
    |> PrincipalReviewer
```

The goal is not just to produce a plan but to challenge it.

### Tradeoff analysis

```fsharp
mission ChooseArchitecture =
    OptionGenerator
    |> TradeoffAnalyst
    |> ScenarioModeler
    |> DecisionAdvisor
```

### Meta-advisory (future)

A meta expert that helps design missions from a problem statement:

```fsharp
expert MetaAdvisor =
    ProblemFramer
    |> ThinkingModelSelector
    |> MissionDesigner
    |> MissionReviewer
```

Given:
```text
I need to migrate 300 Terraform modules to a new platform.
```

Suggests:
```fsharp
mission TerraformMigration =
    DiscoveryAnalyst
    |> DependencyMapper
    |> MigrationArchitect
    |> RiskReviewer
    |> PrincipalReviewer
```

---

## Testable hypothesis

> Expert composition improves reasoning quality, consistency, and outcomes compared to a single general-purpose prompt.

The first prototype exists to test this. The `build-operator` example is the initial test case: run the same problem through a composed mission and through a single prompt, and compare the output quality, consistency, and reviewability.

---

## MVP scope

The MVP focuses on the language and a minimal runtime. It is not a production agent framework.

### In scope

- Hand-written parser for `.fml` files
- Sequential pipeline execution
- Markdown-backed expert definitions
- One LLM client abstraction
- CLI runner (`fml run`)
- Saved run outputs per step

### CLI

```bash
fml run examples/build-operator/mission.fml --input examples/build-operator/input.md
```

### Output

```text
runs/
  build-operator/
    01-KubernetesArchitect.md
    02-SecurityArchitect.md
    03-PrincipalReviewer.md
    final.md
```

---

## Non-goals

The following are explicitly out of scope for the initial language and runtime:

- Low-level tool call syntax
- Retry and error-handling mechanics
- Model provider selection syntax
- Vector store or retrieval configuration
- Agent loop internals
- Workflow-engine plumbing
- Complex DAG or branching syntax

The language should remain small unless a new construct clearly improves reasoning composition.

---

## Repository structure

```text
forge-mission-language/
  README.md
  src/
    ForgeMission.Core/        # Parser, AST, pipeline runner, LLM client abstraction
    ForgeMission.Cli/         # CLI entrypoint (fml run ...)
  examples/
    build-operator/
      mission.fml
      input.md
      experts/
        KubernetesArchitect.md
        SecurityArchitect.md
        PrincipalReviewer.md
  runs/                       # gitignored — output of fml run
```

---

## Implementation plan

### Phase 1 — Language and parser

- Define the `.fml` grammar (`mission`, `expert`, `|>`)
- Write a hand-written recursive-descent parser in C#
- Produce an AST: `MissionDeclaration`, `ExpertDeclaration`, `Pipeline`
- Write unit tests for the parser

### Phase 2 — Expert loader

- Load expert markdown files by name from an `experts/` directory
- Parse YAML frontmatter (`name`, `input`, `output`)
- Validate that all experts referenced in a mission exist

### Phase 3 — Pipeline runner

- Execute the pipeline sequentially
- Pass the previous expert's output as context to the next
- Write each step's output to `runs/<mission-name>/NN-<ExpertName>.md`

### Phase 4 — LLM client

- Abstract the LLM call behind a single interface (`ILlmClient`)
- Implement one concrete client (Anthropic Claude or Azure OpenAI)
- Inject the expert system prompt and prior output as context

### Phase 5 — CLI

- `fml run <mission.fml> --input <input.md>` — run a mission
- `fml validate <mission.fml>` — check that all experts exist and the pipeline is valid
- `fml list experts` — list available experts in the current directory

### Phase 6 — Validation

- Build the `build-operator` example end-to-end
- Run it against the testable hypothesis
- Document findings

---

## Status

Early prototype. Language design and parser are not yet implemented.
