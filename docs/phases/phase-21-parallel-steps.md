# Phase 21 — Parallel Steps + Named Outputs

> **Status: Done.** Grammar shipped in Phase 25 Spoke 1 (`parallel { }` block).
> Runtime concurrent execution (Task.WhenAll) and named outputs (`{{ExpertName.output}}`)
> shipped in Phase 21 (this phase). Streaming inside parallel groups is intentionally deferred.

## Goal

Add `parallel { }` block syntax for concurrent expert execution and give every step a named slot
in the context bag, enabling fan-out/fan-in pipeline patterns.

---

## Motivation — use cases

### UC-1: Image Analysis Pipeline

An image passes through a sequence of specialised models. Each model annotates a
different dimension of the image independently. A final stage synthesises all annotations.

Example pipeline:

```fsharp
mission ImageAnalysis(image_path) =
    ImageCleaner
    |> [FaceRecogniser, TextExtractor, ObjectClassifier]
    |> ContextSynthesiser
```

All three recognisers share the same cleaned image. They do not depend on each other.
`ContextSynthesiser` reads each one's output by name.

### UC-2: Trading Signal Aggregator

Three independent context analysers assess different market dimensions for the same
ticker. They have no dependency on each other and should run concurrently.

```fsharp
mission TradeSignal(ticker) =
    [MarketContext, StockContext, SectorContext]
    |> SignalSynthesiser
```

- `MarketContext` — overall market trend for the day
- `StockContext` — individual stock movement pattern
- `SectorContext` — how the stock's sector is performing
- `SignalSynthesiser` — reads all three by name, produces a trade decision or alert

---

## Language changes

### Parallel step group

A bracket group runs all listed experts concurrently against the same input context:

```fsharp
[ExpertA, ExpertB, ExpertC]
```

Used as a pipeline stage, it acts as a single step: input flows in, all experts run
in parallel, the pipeline continues only after all have completed (or any one fails).

### Named step outputs

Currently `{{output}}` always holds the previous step's text — each step overwrites it.
With named outputs, every step writes its result into its own slot:

```
{{ExpertA.output}}
{{ExpertB.output}}
{{ExpertC.output}}
```

`{{output}}` is preserved for sequential pipelines (backward compatible — it always
reflects the most recent sequential step's text).

Downstream experts reference named outputs in their system prompt:

```markdown
Market trend: {{MarketContext.output}}
Stock movement: {{StockContext.output}}
Sector performance: {{SectorContext.output}}

Based on the above, produce a trade signal.
```

---

## Runtime model — open questions

Two options for the parallel execution runtime:

**Option A — Task.WhenAll**
Simple. Launch all experts as parallel `Task`s, await all. Cancel remaining if any
one fails (respects existing fail-fast contract). No streaming per parallel step.

**Option B — Channel-based**
Each parallel expert streams tokens into its own channel. Harder to implement but
consistent with Phase 15 streaming behaviour.

Recommendation: start with Option A. Streaming inside a parallel group is a
separate concern and can be layered on later.

---

## AOT considerations

`Task.WhenAll` is AOT-safe. No new reflection or dynamic dispatch required.
The context bag change (adding named keys) is additive — existing string dictionary,
new key naming convention only.

---

## Tasks

- [x] Grammar: `parallel { }` block syntax — shipped in Phase 25 Spoke 1
- [x] AST: `ParallelElement(IReadOnlyList<Step> Steps)` node in `ForgeMission.Parser/Ast.cs`
- [x] PipelineRunner: `Task.WhenAll` concurrent execution, fail-fast via `CancellationTokenSource`
- [x] Context bag: each step result written to `{{ExpertName.output}}`; `{{output}}` unchanged (last sequential step)
- [x] Tests: `ParallelBlock_AllStepsExecute`, `ParallelBlock_NamedOutputsAvailableToDownstreamStep`, `ParallelBlock_OneFails_PipelineFails`

Deferred: streaming inside parallel groups (channels); trading signal demo mission.
