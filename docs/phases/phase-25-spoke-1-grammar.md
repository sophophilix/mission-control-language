# Phase 25 — Spoke 1: Grammar

## Status: Todo

## Changes

### 1. Replace `|>` with `->`

`->` means "passes to" — neutral, directional, no prior art baggage. Developers read it correctly on first encounter without knowing MCL.

**Before:**
```fsharp
mission BuildOperatorDesign(goal, persona) =
    KubernetesArchitect
    |> SecurityArchitect
    |> PrincipalReviewer with { style = "terse ADR" }
```

**After:**
```fsharp
mission BuildOperatorDesign(goal, persona) =
    KubernetesArchitect
    -> SecurityArchitect
    -> PrincipalReviewer with { style = "terse ADR" }
```

### 2. Add `parallel { }` block

Parallelism is declared on the container, not inferred from an operator. Experts inside the block run concurrently. The `->` before and after the block means the same thing it always does — sequential hand-off.

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

Each parallel expert's output is available in subsequent steps as `{{ExpertName}}` — e.g. `{{Summariser}}`, `{{FactChecker}}`, `{{Critic}}`. No explicit naming required in the common case. The `as` keyword is reserved for the edge case where the same expert appears twice in one parallel block.

### 3. Remove OCI expert declaration syntax from `.mcl`

`expert … from … version …` declarations move entirely to `forge.toml`. The grammar no longer accepts them in `.mcl` files.

**Before (in mission.mcl):**
```fsharp
expert KubernetesArchitect =
    from "ghcr.io/katasec/forge-kubernetes-architect"
    version "0.1.0"
```

**After:** not valid in `.mcl`. Declared in `forge.toml` instead.

Local experts (directory-based) remain valid — they are resolved by name without any declaration in `.mcl`.

## ANTLR grammar changes

- Replace `PIPE : '|>' ;` token with `ARROW : '->' ;`
- Update `pipeline` rule to use `ARROW`
- Add `parallelBlock` rule: `'parallel' '{' UPPER_ID+ '}'`
- Update `step` rule to accept `parallelBlock` as an alternative
- Remove `expertSource` rule (`from`, `version` keywords and their productions)
- Remove `FROM`, `VERSION` tokens

After grammar changes, regenerate the parser:
```bash
java -jar /tmp/antlr4-4.13.1-complete.jar -Dlanguage=CSharp -package ForgeMission.Core.Parser \
     -visitor -o src/ForgeMission.Core/Parser/Generated \
     src/ForgeMission.Core/Parser/FmlGrammar.g4
```

## AST changes

- `Pipeline` node: no structural change, operator is implicit
- Add `ParallelBlock` node: list of `Identifier` nodes
- Remove `ExpertSource` node (was `from`/`version` on expert declarations)

## Test gate

All existing parser tests must pass with `->` substituted for `|>`. New tests for `parallel { }` parsing and rejection of `from`/`version` in `.mcl`.
