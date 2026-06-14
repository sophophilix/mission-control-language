# FML — Language Design

## Primitives

The language has exactly three primitives. This is intentional — the language should remain small unless a new construct clearly improves reasoning composition.

| Primitive | Meaning |
|-----------|---------|
| `mission` | A problem or desired outcome |
| `expert`  | A reusable reasoning capability |
| `\|>`      | Progressive refinement / expert composition |

## Grammar (BNF)

```bnf
program     = declaration*
declaration = mission | expert

mission     = "mission" identifier "=" pipeline
expert      = "expert"  identifier "=" pipeline

pipeline    = identifier ("|>" identifier)*
identifier  = [A-Z][A-Za-z0-9]*
```

Seven rules. This is the entire language.

## Syntax decisions

### F#-inspired, not F#

FML borrows F# syntax because the grammar is already well-specified, the `|>` pipe operator is culturally loaded with exactly the right meaning (progressive transformation), and developers can read it without learning anything new.

FML does not embed in F# and does not use F# semantics. The `|>` operator in FML means sequential reasoning refinement, not function composition. This divergence is intentional and documented to avoid confusion.

### Capitalisation

| Element | Convention | Reason |
|---------|-----------|--------|
| Keywords (`mission`, `expert`) | lowercase | Keywords are language machinery — they should recede visually. Matches every language convention: `if`, `for`, `class`. |
| Identifiers (`KubernetesArchitect`) | PascalCase | Experts are proper nouns representing roles. PascalCase signals agency and creates immediate visual distinction from keywords. |

Both conventions are enforced by the grammar, not style guidelines. A lowercase identifier or uppercase keyword is a parse error.

### Strict subset

The following F# constructs are explicitly excluded:

- `let`, `type`, `module`, `open`
- Lambdas, expressions, literals
- Whitespace sensitivity
- Type annotations
- Match expressions

Nothing is added to the language unless it clearly improves reasoning composition.

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
- Retry logic
- Model provider selection
- Vector store configuration
- Agent loop internals
- DAG or branching syntax
- Parameters or configuration

These live in the runtime layer or below. The language expresses only reasoning structure.
