# MCL — Architecture

## Guiding principle

The direct IChatClient adapter is the only LLM integration point. It does not appear above the adapter layer. The parser, AST, pipeline runner, and CLI know nothing about the underlying provider.

## Execution phases

Every `forge run` passes through three distinct phases. Each has a single responsibility and a clear failure mode.

```
Source text  (.mcl file)
     ↓
  [ Parse ]      — syntax only. No file system, no registry, no LLM.
     ↓            Produces an AST with unresolved ExpertRef nodes.
    AST
     ↓
  [ Resolve ]    — binds each ExpertRef name to an ExpertDefinition.
     ↓            Local-first, then global cache. Fails fast if any name is missing.
 Bound AST
     ↓
  [ Execute ]    — walks the bound pipeline, calls IExpertRunner per step.
                   Never deals with names or strings again.
```

### Why this separation matters

- **Parse errors** and **resolution errors** are distinct — wrong message in the wrong phase is confusing to diagnose.
- `forge validate` can parse and check syntax without pulling experts from cache.
- The parser is pure — no DI, no file system, fully unit-testable with strings alone.
- `forge run --verbose` reports resolution source (local vs cache) because resolution is a named phase with its own output.

### ExpertRef — unresolved at parse time

`KubernetesArchitect` in a mission file is a PascalCase identifier (`UPPER_ID` token). The parser wraps it in an `ExpertRef` node — just a string. The parser succeeds if the syntax is valid. It does not check whether the expert exists.

```csharp
record ExpertRef(string Name);  // "KubernetesArchitect" — name only, nothing resolved
```

The Resolver is the only component that turns an `ExpertRef` into an `ExpertDefinition`. The Runtime never sees unresolved names.

### AST node shapes

```csharp
record Program(IReadOnlyList<Declaration> Declarations);

abstract record Declaration;
record MissionDeclaration(
    string Name,                        // "BuildOperatorDesign"
    IReadOnlyList<string> Params,       // ["goal", "persona"]
    Pipeline Pipeline
) : Declaration;

record Pipeline(IReadOnlyList<PipelineElement> Elements);

abstract record PipelineElement;
record Step(ExpertRef Expert, WithClause? With) : PipelineElement;
record ParallelBlock(IReadOnlyList<Step> Steps) : PipelineElement;

record ExpertRef(string Name);
record WithClause(IReadOnlyDictionary<string, string> Bindings);
```

Example AST for:
```fsharp
mission BuildOperatorDesign(goal, persona) =
    KubernetesArchitect with { provider = "architect" }
    -> SecurityArchitect
    -> PrincipalReviewer
```

```
MissionDeclaration "BuildOperatorDesign" (goal, persona)
  └── Pipeline
        ├── Step
        │     ├── ExpertRef "KubernetesArchitect"   ← unresolved name
        │     └── WithClause { provider = "architect" }
        ├── Step
        │     └── ExpertRef "SecurityArchitect"
        └── Step
              └── ExpertRef "PrincipalReviewer"
```

---

## Components

### 1. Parser — `ForgeMission.Core/Parser/`

Pure C#, zero external dependencies. Takes a `.mcl` file as a string, produces an AST.

```
Lexer → TokenStream → Parser → AST
```

**Testable in isolation:** unit tests with string inputs only, no disk, no LLM.

---

### 2. Resolver — `ForgeMission.Core/Experts/`

Walks the AST and binds each `ExpertRef` to an `ExpertDefinition` by reading `expert.md` files. Resolution is eager — all names resolved before execution begins. Fails fast with a clear error if any name is missing.

Resolution order per expert name:
1. `<mission-dir>/experts/<Name>/expert.md` — local, always wins
2. `~/.forge/experts/<registry>/<Name>@<version>/` — global cache
3. Error — not found, tell user to run `forge init`

```csharp
record ExpertDefinition(string Name, string SystemPrompt, string Input, string Output);
```

**Testable in isolation:** unit tests with fixture markdown files.

---

### 3. Pipeline Runner — `ForgeMission.Core/Runtime/`

Orchestration loop. Walks the pipeline in order, calls `IExpertRunner` for each step, passes the output of step N as input to step N+1, writes each step's output to `runs/<mission-name>/NN-<ExpertName>.md`.

```csharp
interface IExpertRunner
{
    Task<string> RunAsync(ExpertDefinition expert, string context, CancellationToken ct);
}
```

Knows nothing about MAF. Depends only on `IExpertRunner`.

**Testable in isolation:** unit tests with a stub `IExpertRunner` returning canned strings.

---

### 4. Direct IChatClient Adapter — `ForgeMission.Core/Adapters/DirectExpertRunner.cs`

The single place provider interaction exists in the codebase. Implements `IExpertRunner`. Builds a `[System, User]` message list, calls `IChatClient.CompleteAsync()` (or `CompleteStreamingAsync()` for streaming), deserialises the JSON response into `StepEnvelope`.

One file. Swappable without touching anything else. No MAF dependency.

**Testable:** integration test against a real LLM.

---

### 5. CLI — `ForgeMission.Cli/`

Thin entry point. Parses arguments, wires up dependencies via DI, delegates to Core. No business logic.

**Commands:**

| Command | Description |
|---------|-------------|
| `forge init [mission.mcl]` | Resolve expert sources and generate mcl.lock |
| `forge run [mission.mcl]` | Run a mission (self-contained — no input file required) |
| `forge validate [mission.mcl]` | Validate all experts exist and pipeline is well-formed |
| `forge list experts` | List available experts |
| `forge expert init <Name>` | Scaffold a new expert directory |

---

## Dependency flow

```
CLI
 └→ Pipeline Runner
      └→ Parser           (produces AST)
      └→ Expert Loader    (resolves markdown)
      └→ IExpertRunner
           └→ Direct IChatClient Adapter (hidden — only impl of IExpertRunner)
```

Strictly one direction. Nothing flows upward.

## Project references

```
ForgeMission.Cli
  └→ ForgeMission.Core

ForgeMission.Core
  └→ Microsoft.Extensions.AI   (IChatClient abstraction — adapter only)
```

## Output structure

```
runs/
  <mission-name>/
    01-<ExpertName>.md
    02-<ExpertName>.md
    ...
    final.md
```

Each step output is written before the next step begins, so a partial run is always inspectable.
