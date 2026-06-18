# FML — Language Design

## Primitives

The language has exactly three primitives. This is intentional — the language should remain small unless a new construct clearly improves reasoning composition.

| Primitive | Meaning |
|-----------|---------|
| `mission` | A problem or desired outcome |
| `expert`  | A reusable reasoning capability |
| `\|>`      | Progressive refinement / expert composition |

## Grammar

The authoritative grammar is [`src/ForgeMission.Core/Parser/FmlGrammar.g4`](../../src/ForgeMission.Core/Parser/FmlGrammar.g4). The ANTLR4 tool generates the lexer and parser from this file.

```antlr
grammar FmlGrammar;

program    : (letBinding | declaration)* EOF ;
letBinding : 'let' LOWER_ID '=' value ;
declaration : mission | expert ;
mission    : 'mission' UPPER_ID params? '=' pipeline ;
expert     : 'expert' UPPER_ID params? '=' pipeline ;
params     : '(' LOWER_ID (',' LOWER_ID)* ')' ;
pipeline   : step ('|>' step)* ;
step       : UPPER_ID withClause? ;
withClause : 'with' '{' binding (',' binding)* '}' ;
binding    : LOWER_ID '=' value ;
value      : STRING | LOWER_ID | envCall ;
envCall    : 'env' '(' STRING (',' STRING)? ')' ;

MISSION  : 'mission' ; EXPERT : 'expert' ; LET : 'let' ; WITH : 'with' ; ENV : 'env' ;
PIPE     : '|>'      ; EQUALS : '='      ;
LPAREN   : '('       ; RPAREN : ')'      ;
LBRACE   : '{'       ; RBRACE : '}'      ;
COMMA    : ','       ;
UPPER_ID : [A-Z][a-zA-Z0-9]* ;
LOWER_ID : [a-z][a-zA-Z0-9]* ;
STRING   : '"' (~["\r\n])* '"' ;
WS       : [ \t\r\n]+ -> skip ;
```

To regenerate the parser after a grammar change:
```
java -jar /tmp/antlr4-4.13.1-complete.jar -Dlanguage=CSharp -package ForgeMission.Core.Parser \
     -visitor -o src/ForgeMission.Core/Parser/Generated \
     src/ForgeMission.Core/Parser/FmlGrammar.g4
# Then move generated files out of the nested path:
# cp src/ForgeMission.Core/Parser/Generated/src/ForgeMission.Core/Parser/*.cs \
#    src/ForgeMission.Core/Parser/Generated/
# rm -rf src/ForgeMission.Core/Parser/Generated/src
```

## Syntax decisions

### The `->` pipeline operator

MCL uses `->` ("passes to") as the sequential composition operator.

`->` is directional and neutral — a developer who has never seen MCL reads it correctly on first encounter. It means "the output of this step becomes the input of the next."

`|>` was considered (F# pipe-forward) but rejected: F# developers expect `f |> g` to mean `g(f)` — function composition — which is subtly different from expert composition. `->` carries no prior-art semantics and does not create a false analogy.

### `parallel { }` block

Parallelism is declared on the container, not inferred from an operator between items. Experts inside the block run concurrently. `->` before and after the block means the same thing it always does — sequential hand-off.

```fsharp
mission Analysis(input) =
    DataExtractor
    -> parallel {
        Summariser
        FactChecker
        Critic
    }
    -> Synthesiser
```

Each parallel expert's output is available in subsequent steps as `{{ExpertName}}` — the expert name is the variable name. The `as` keyword exists for the rare case where the same expert appears twice in one parallel block and names would collide; it is not needed in the common case.

### F#-inspired, not F#

MCL borrows F# aesthetic (clean, expression-oriented, minimal punctuation) but does not embed in F# and does not use F# semantics. The `->` operator means sequential reasoning hand-off, not function application.

### Capitalisation

| Element | Convention | Reason |
|---------|-----------|--------|
| Keywords (`mission`, `expert`, `let`, `with`, `env`) | lowercase | Keywords are language machinery — they should recede visually. Matches every language convention: `if`, `for`, `class`. |
| Expert identifiers (`KubernetesArchitect`) | PascalCase | Experts are proper nouns representing roles. PascalCase signals agency and creates immediate visual distinction from keywords. |
| Variable identifiers (`goal`, `persona`) | camelCase | Variables are bindings, not roles — lowercase signals data rather than agent. |

Both identifier conventions are enforced by the grammar, not style guidelines. A lowercase expert name or uppercase variable is a parse error.

### Variables and context

`let` bindings declare constants that seed the context bag at mission start. The context bag
(`Dictionary<string, object>`) is the OWIN `AppFunc` analogy: each expert reads what it needs
and the `output` key carries the chained result forward.

```fsharp
let goal    = "Design a production-grade K8s build operator"
let apiKey  = env("OPENAI_API_KEY")           // read from process environment
let model   = env("FML_MODEL", "gpt-4o-mini") // with default

mission BuildOperator(goal) =
    KubernetesArchitect
    |> PrincipalReviewer with { style = "terse ADR" }
```

Expert system prompts use `{{key}}` placeholders interpolated from the context bag before each
step runs.

Variable resolution order (lowest → highest precedence):

1. `let` binding
2. `with { }` clause on a step
3. `--var key=value` CLI flag

### Reserved context variables

A small set of variables are injected by the runtime and available to every expert in every
mission. They cannot be overridden by `let` bindings or `--var`.

| Variable | Set by | Value |
|----------|--------|-------|
| `{{output}}` | Runtime, after each step | The previous step's text output. Empty string on the first step. |
| `{{attempt}}` | Runtime, at the start of each loop iteration | Current attempt number, 1-based. Always `1` for missions without `loop`. |
| `{{max_loops}}` | Runtime, from the mission's `loop N` declaration | Declared loop cap. Always `1` for missions without `loop`. |
| `{{ExpertName}}` | Runtime, after each parallel step | Output of a named expert inside a `parallel { }` block. E.g. `{{Summariser}}`, `{{FactChecker}}`. Not set for sequential steps. |
| `{{feedback}}` | Runtime, at the start of each loop iteration from attempt 2 onward | The Judge's structured failure reason from the previous iteration. Empty string on attempt 1. Enables deterministic convergence — each expert in the chain knows what failed and why. |

These are the only reserved variables. The set is intentionally minimal — everything else
comes from `let` bindings or `--var`. A new reserved variable requires a language design
decision, not just a runtime change.

### Domain variables vs infrastructure variables

Not all `let` bindings belong in `mission.mcl`. The rule is:

> **Would this variable appear in an expert's system prompt?**
> - Yes → it is a domain variable. It belongs in `mission.mcl`.
> - No → it is an infrastructure variable. It belongs in `forge.toml`.

Domain variables (`goal`, `persona`, `product`) are reasoning inputs. They flow through the context bag and into expert prompts via `{{key}}` placeholders. They belong close to the mission that uses them.

Infrastructure variables (`provider`, `apiKey`, `model`, `endpoint`) configure the LLM runtime. They never appear in a prompt. They belong in `forge.toml` as named provider profiles, not in the mission file.

This separation keeps `mission.mcl` a pure reasoning artifact — readable without knowing anything about the infrastructure running it.

### Reserved binding names (infrastructure — moved to `forge.toml`)

Provider configuration is declared in `forge.toml` as named profiles, not as `let` bindings in `mission.mcl`. See [Phase 25 — forge.toml](../phases/phase-25-spoke-2-forge-toml.md) for the schema.

The four formerly-reserved binding names (`provider`, `apiKey`, `model`, `endpoint`) are no longer declared in `.mcl` files. They are infrastructure, not reasoning.

Mission authors declare these as standard `let` bindings using `env()`. The canonical form:

```fsharp
let provider = env("MCL_PROVIDER", "openai")
let apiKey   = env("MCL_API_KEY")
let model    = env("MCL_MODEL", "gpt-4o-mini")
// endpoint is omitted unless overriding the provider default
```

The `env()` call is a convention, not a requirement — authors may hardcode values or use
any env var name they choose. The canonical `MCL_*` names are the recommended defaults.

Per-provider default endpoints are maintained in the runtime's provider lookup table (see
`docs/phases/phase-17-provider-config.md`). Azure has no universal default — `endpoint`
must be declared when `provider = "azure"`.

### Strict subset

The following constructs are explicitly excluded:

- `type`, `module`, `open`
- Lambdas, expressions (beyond string literals and env() calls)
- Whitespace sensitivity
- Type annotations
- Match expressions
- Mutable state

Nothing is added to the language unless it clearly improves reasoning composition.

## One mission per file

Every `.mcl` file encodes exactly one thinking model. This is not an observed pattern — it is a design constraint.

A `.mcl` file is to a mission what an `expert.md` file is to an expert: one file, one unit. Allowing multiple missions per file would pull MCL toward being a module system, which is outside the language's scope.

This constraint makes the agent mapping trivial: one `.mcl` → one mission → one agent → one endpoint. No disambiguation is needed.

Note: a file may contain multiple `expert` declarations alongside the single `mission` — that is expected and correct. Only the number of `mission` declarations is constrained to one.

## Recursive composition

Experts can be composed from other experts, giving the language recursive decomposition:

```fsharp
expert KubernetesArchitect =
    RequirementsAnalyst
    |> PlatformArchitect
    |> ReliabilityArchitect
```

This means a high-level expert is itself a pipeline. The runtime resolves expert references recursively before execution.

## What the language does not express

The following are out of scope at the language level:

- Tool calls
- Model provider selection (beyond `env("FML_MODEL")`)
- Vector store configuration
- Agent loop internals
- DAG or branching syntax

Note: `loop N` on a mission declaration is in scope — it is a bounded retry up to N attempts
until all steps pass. Unbounded loops, conditional branching, and DAG execution are not.

These live in the runtime layer or below. The language expresses only reasoning structure and
the context that flows through it.
