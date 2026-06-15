# MCL Use Cases

Concrete scenarios collected during design discussions. Used to validate language features
and prioritise phases.

---

## UC-1 — Image Analysis Pipeline

**Domain:** Computer vision / document processing

**Scenario:** An image passes through a sequence of specialised models. Each model
annotates a different dimension of the image. A final stage synthesises all annotations
into a unified result.

Example stages:
- Image cleaner / preprocessor
- Face and person recogniser
- OCR / text extractor
- Object / scene classifier
- Context synthesiser (LLM)

**Key language needs:**
- All stages share the original artifact, not just the previous stage's text output
- Each stage adds named annotations to the context bag independently
- The synthesiser can reference each upstream output by name (not just `{{output}}`)

**Language features required:** Named step outputs (Phase 21), and eventually
non-LLM expert kinds (Phase 22) for the vision model stages.

---

## UC-2 — Trading Signal Aggregator

**Domain:** Quantitative finance / market analysis

**Scenario:** Three independent context analysers assess different market dimensions
for the same ticker. They do not depend on each other and should run concurrently.
A final synthesiser combines the signals into a trade decision or alert.

Parallel stages:
- Market context (overall market trend for the day)
- Stock context (individual stock movement pattern)
- Sector context (how the stock's sector is performing)

Final stage:
- Signal synthesiser (LLM) — reads all three outputs by name

**Key language needs:**
- Parallel step execution — `[A, B, C]` runs concurrently, not sequentially
- Named outputs — synthesiser distinguishes `{{MarketContext.output}}` from
  `{{StockContext.output}}` and `{{SectorContext.output}}`
- Fan-in — synthesiser receives all three before proceeding

**Language features required:** Parallel steps (Phase 21).

---

## UC-3 — Production Log Anomaly Detection

**Domain:** Observability / SRE / production debugging

**Scenario:** Log streams from a production system feed into a pipeline. A classical
ML scoring model flags unusual patterns (spike in error rate, latency outlier, correlated
failures across services). A downstream LLM expert takes the flagged anomalies and
produces a human-readable root cause analysis and remediation suggestion.

Example stages:
- Log parser / normaliser (LLM or rule-based)
- Anomaly detector (ONNX / sklearn scoring model — numeric input, anomaly score output)
- Root cause analyst (LLM — reads anomaly score and flagged log lines)
- Incident reporter (LLM — writes structured incident summary)

**Key language needs:**
- A pipeline stage that is not LLM-backed — reads typed numeric values from the
  context bag, writes a score back
- Expert frontmatter `kind` field to declare the runner type (`llm`, `onnx`, `http`)
- The context bag carries typed values, not just strings, between stages

**Language features required:** Non-LLM expert kinds (Phase 22).

---

## Common patterns across use cases

| Pattern | Use cases | Phase |
|---------|-----------|-------|
| Named per-step outputs in context bag | UC-1, UC-2, UC-3 | Phase 21 |
| Parallel step execution `[A, B, C]` | UC-2 | Phase 21 |
| Shared upstream artifact accessible to all stages | UC-1 | Phase 21 |
| Non-LLM expert kinds (`kind: onnx`, `kind: http`) | UC-3 | Phase 22 |
| Typed context bag values (numeric, binary) | UC-3 | Phase 22 |
