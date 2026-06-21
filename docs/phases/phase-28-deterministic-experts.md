# Phase 28 — Deterministic Experts & Rule Stdlib

## Status: Design (after Phase 22a)

## Motivation

LLM judges are powerful but non-deterministic. They hedge. They occasionally miss
obvious constraints. A 200-word limit enforced by a judge will sometimes let 210 words
through with a "pass — slightly over but acceptable" rationale.

Structural constraints should be enforced structurally. Gary Marcus's neurosymbolic
argument applies directly: push determinism left in the pipeline, push LLM judgment
as far right as possible.

```
rule experts        ← deterministic gates: word count, JSON validity, format checks
      ↓
LLM critics         ← qualitative review: tone, clarity, persuasiveness
      ↓
LLM judges          ← holistic pass/fail: "would a customer buy this?"
```

A `kind: rule` expert runs in-process, returns a result in microseconds, has zero
token cost, and is guaranteed to enforce its constraint on every invocation.

## Language changes

### `kind: rule` in expert frontmatter

```markdown
---
name: WordCountCheck
kind: rule
check: word_count < 200
onFail: "Output exceeds 200 words. Revise for brevity."
---
```

No system prompt body required — `ExpertLoader` skips prompt file resolution for
`kind: rule`. `check` is a string parsed by a dedicated rule expression evaluator
(not the ANTLR grammar).

`onFail` is camelCase. (YamlDotNet does not auto-map hyphenated keys — `on-fail`
would silently be ignored.)

`kind` defaults to `llm` when absent — fully backward compatible.

### Compound expressions

```markdown
check: word_count < 200 and json_parseable
```

`and` is supported. `or` is deferred — quality gates are conjunctions; disjunctions
signal ambiguous requirements.

### Loop convergence tie-in

The `onFail` message from a rule expert is the structured feedback injected into
the next loop iteration's first expert — exactly as a judge's `reason` field is today.
Rule failures give more precise, actionable feedback than LLM judge failures because
they are deterministic and not hedged.

```fsharp
mission RefinedOutput(goal) loop(3) = {
    Drafter
    -> WordCountCheck      // deterministic gate — fails fast if > 200 words
    -> QualityJudge        // holistic gate — subjective assessment
}
```

On retry, `WordCountCheck`'s `onFail` message ("Output exceeds 200 words. Revise
for brevity.") is prepended to `Drafter`'s context. The LLM gets a precise,
machine-generated instruction, not a hedged LLM critique.

## Standard library evaluators

### Text checks

| Evaluator | Example | Description |
|-----------|---------|-------------|
| `word_count` | `word_count < 200` | Whitespace-delimited token count |
| `char_count` | `char_count < 1000` | UTF-16 character count |
| `sentence_count` | `sentence_count > 3` | Heuristic sentence boundary detection |
| `line_count` | `line_count < 50` | Newline-delimited line count |
| `contains` | `contains "## Summary"` | Literal substring match |
| `contains_pattern` | `contains_pattern "^#{1,2} "` | Regex match |
| `starts_with` | `starts_with "{"` | Prefix check |
| `ends_with` | `ends_with "}"` | Suffix check |
| `no_match` | `no_match "TODO\|FIXME"` | Fails if pattern found |
| `reading_level` | `reading_level < 12` | Flesch-Kincaid grade level (best-effort statistical approximation) |

### Structure checks

| Evaluator | Example | Description |
|-----------|---------|-------------|
| `json_parseable` | `json_parseable` | Valid JSON (no argument) |
| `xml_parseable` | `xml_parseable` | Valid XML (no argument) |
| `markdown_has_heading` | `markdown_has_heading` | At least one `#` heading present |
| `schema_valid` | `schema_valid "./schemas/output.json"` | JSON Schema validation against a file |

## RuleExpertRunner

Synchronous, in-process, no network, no LLM. Implements `IExpertRunner`.

```csharp
class RuleExpertRunner : IExpertRunner
{
    // Parses `check` expression, evaluates against step output, returns StepEnvelope.
    // Pass → { status: "pass" }
    // Fail → { status: "fail", reason: expert.OnFail }
}
```

Returns a `StepEnvelope` — transparent to `PipelineRunner` and the loop convergence
mechanism. `PipelineRunner` does not need to know the expert is a rule, not an LLM.

AOT considerations: expression evaluation is a static switch on evaluator name — no
reflection, no dynamic dispatch. All evaluators are registered at compile time.

## What is explicitly out of scope

- **Custom assembly loading** (`kind: rule` + `assembly = "./foo.dll"`) — AOT-incompatible.
  Custom deterministic logic uses `kind: http` (Phase 22a) instead — call a local HTTP
  endpoint that runs arbitrary code.
- **`or` compound expressions** — deferred. Quality gates are conjunctions.
- **Stateful evaluators** — each rule runs against the current step output only.
  Cross-step state belongs in the context bag, not in rule experts.

## Future composition pattern (not this phase)

`when()` routing from rule expert status enables conditional repair chains:

```fsharp
-> WordCountCheck
-> Condenser when(WordCountCheck.status: "fail")
-> QualityJudge
```

This requires the typed context bag and `when()` expression extension — deferred.

## Dependencies

- **Phase 22a (Kind Dispatch Infrastructure)** — hard prerequisite. `RuleExpertRunner`
  plugs into the `RunnerFor` dispatch switch introduced in 22a.
- Phase 25 Spoke 3 (Expert Resolution) — Done. `ExpertLoader` already has the seam
  to skip the prompt file for non-`llm` kinds.

## Tasks

- [ ] Add `check` and `onFail` to `ExpertFrontmatter` (alongside `kind` from Phase 22a)
- [ ] Implement rule expression parser — static switch on evaluator name + argument
- [ ] Implement text evaluators: `word_count`, `char_count`, `sentence_count`, `line_count`, `contains`, `contains_pattern`, `starts_with`, `ends_with`, `no_match`, `reading_level`
- [ ] Implement structure evaluators: `json_parseable`, `xml_parseable`, `markdown_has_heading`, `schema_valid`
- [ ] Implement `RuleExpertRunner` — evaluate, return `StepEnvelope`
- [ ] Wire `onFail` into loop feedback injection (same path as judge `reason`)
- [ ] Tests: each evaluator, compound `and`, pass/fail envelope, loop retry receives `onFail` message
- [ ] Demo: add `WordCountCheck` to an existing mission to show the deterministic gate in action
- [ ] Docs: update `language.md` expert frontmatter section with `kind: rule` reference
