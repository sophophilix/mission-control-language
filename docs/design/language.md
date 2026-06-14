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

### F#-inspired, not F#

FML borrows F# syntax because the grammar is already well-specified, the `|>` pipe operator is culturally loaded with exactly the right meaning (progressive transformation), and developers can read it without learning anything new.

FML does not embed in F# and does not use F# semantics. The `|>` operator in FML means sequential reasoning refinement, not function composition. This divergence is intentional and documented to avoid confusion.

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

### Strict subset

The following constructs are explicitly excluded:

- `type`, `module`, `open`
- Lambdas, expressions (beyond string literals and env() calls)
- Whitespace sensitivity
- Type annotations
- Match expressions
- Mutable state

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
- Model provider selection (beyond `env("FML_MODEL")`)
- Vector store configuration
- Agent loop internals
- DAG or branching syntax

These live in the runtime layer or below. The language expresses only reasoning structure and
the context that flows through it.
