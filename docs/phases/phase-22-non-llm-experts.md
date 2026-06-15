# Phase 22 — Non-LLM Expert Kinds

## Goal

Allow pipeline stages backed by something other than an LLM — ONNX scoring models,
HTTP scoring endpoints, or rule-based processors — using a `kind` field in expert
frontmatter. The runner dispatches based on `kind` without reflection.

---

## Motivation — use case

### UC-3: Production Log Anomaly Detection

Log streams from a production system feed into a pipeline. A classical ML scoring
model flags unusual patterns. A downstream LLM expert produces a human-readable
root cause analysis.

```fsharp
mission LogAnomalyAnalysis(log_stream) =
    LogParser
    |> AnomalyDetector
    |> RootCauseAnalyst
    |> IncidentReporter
```

- `LogParser` — LLM or rule-based; normalises raw logs into structured context keys
- `AnomalyDetector` — ONNX model; reads numeric context keys, writes anomaly score
- `RootCauseAnalyst` — LLM; reads `{{AnomalyDetector.output}}` and flagged log lines
- `IncidentReporter` — LLM; writes structured incident summary

The anomaly detector is not LLM-backed. It is a trained scoring model that takes
numeric feature values and returns a score and a pass/fail decision.

---

## Language changes

### `kind` field in expert frontmatter

```markdown
---
name: AnomalyDetector
kind: onnx
model: ./models/isolation-forest.onnx
inputs: [cpu_usage, memory_usage, request_latency]
output-key: anomaly_score
threshold: 0.85
input: Normalised metric features
output: Anomaly score and pass/fail decision
---
```

`kind` defaults to `llm` when absent — fully backward compatible.

Supported kinds:

| Kind | Runner | Description |
|------|--------|-------------|
| `llm` | `DirectExpertRunner` | Default. IChatClient-backed, system prompt required. |
| `onnx` | `OnnxExpertRunner` | Loads an ONNX model, reads named float keys from context, writes score back. |
| `http` | `HttpExpertRunner` | POSTs context as JSON to an external scoring endpoint, reads response. |

### Context bag typing

Currently all context values are strings. Non-LLM stages need numeric and binary values.

Proposed approach: keep the bag as `Dictionary<string, object>`. Numeric values are
stored as `double`. String serialisation for LLM prompts (via `{{key}}`) converts
to string automatically. ONNX runner reads `double` directly.

This avoids a breaking change — existing string values continue to work unchanged.

---

## Runner dispatch

Dispatch is a static switch on the `kind` string read from frontmatter — no reflection,
no `Activator.CreateInstance`. AOT-safe by construction:

```csharp
IExpertRunner RunnerFor(string kind) => kind switch
{
    "llm"  => new DirectExpertRunner(chatClient),
    "onnx" => new OnnxExpertRunner(),
    "http" => new HttpExpertRunner(),
    _      => throw new ExpertLoadException($"Unknown expert kind '{kind}'")
};
```

---

## AOT considerations

- `Microsoft.ML.OnnxRuntime` has AOT-compatible packages — verify before committing
- HTTP runner uses `HttpClient` — AOT-safe
- No new reflection required; `kind` dispatch is a compile-time switch
- Any new types flowing through STJ serialisation need source-gen contexts per CLAUDE.md

---

## Dependencies

- Phase 21 named outputs (`{{Name.output}}`) should land first — `RootCauseAnalyst`
  needs `{{AnomalyDetector.output}}` to be meaningful
- OnnxRuntime package evaluation needed before tasks are scheduled

---

## Tasks

- [ ] Evaluate `Microsoft.ML.OnnxRuntime` AOT compatibility
- [ ] Add `kind`, `model`, `inputs`, `output-key`, `threshold` to `ExpertFrontmatter`
- [ ] Implement `OnnxExpertRunner` — load model, extract float features, run inference
- [ ] Implement `HttpExpertRunner` — POST context JSON, parse response into StepEnvelope
- [ ] `ExpertLoader`: read `kind`, select runner accordingly
- [ ] Context bag: allow `double` values alongside strings
- [ ] Tests: ONNX runner with a simple test model, HTTP runner with a mock endpoint
- [ ] Demo: log anomaly detection mission (can use a simple mock ONNX model for demo)
