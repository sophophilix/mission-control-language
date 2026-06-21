# Phase 29 — UC Reference Missions

> **Status: Todo**  
> **Depends on:** Phase 22b (ONNX) for UC-3. UC-1 and UC-2 can proceed before ONNX lands.  
> **Purpose:** Prove the language against real customer use cases. Use cases are the
> validation instrument — they tell us whether the primitives are sufficient, composable,
> and expressive enough to do real work.

---

## Background — use case methodology

MCL primitives were built with three customer use cases as the validation instrument.
Each use case stress-tested a different capability dimension:

| Use case | Dimension validated | Status |
|----------|--------------------|----|
| UC-1: Image Analysis | `parallel {}`, named outputs, fan-out/fan-in | Language ready; demo mission needed |
| UC-2: Trading Signal | `parallel {}`, named outputs, synthesiser pattern | Language ready; demo mission needed |
| UC-3: Log Anomaly Detection | Non-LLM stages (`kind: onnx`), typed context bag | Blocked on Phase 22b |
| UC-4: Project Assistant | Mission composition, `forge serve`, self-hosting | Deferred to last — this is the self-use case |

The approach: build UC-1 and UC-2 demo missions immediately (language is ready), build
UC-3 after Phase 22b lands (ONNX dependency), then move to UC-4.

---

## Hub + Spokes

### Spoke 1 — UC-1: Image Analysis Pipeline

**Source:** `docs/phases/phase-21-parallel-steps.md` → UC-1

**Design:**
```fsharp
mission ImageAnalysis(image_path) = {
    ImageCleaner
    -> parallel {
        FaceRecogniser
        TextExtractor
        ObjectClassifier
    }
    -> ContextSynthesiser
}
```

- `ImageCleaner` — LLM expert; normalises image metadata / description for downstream experts
- `FaceRecogniser` — `kind: http` calling an external vision API (Azure Vision, Google Vision);
  returns face detection results as JSON
- `TextExtractor` — `kind: http` calling an OCR API; returns extracted text
- `ObjectClassifier` — `kind: http` calling an object detection API; returns labels + confidence
- `ContextSynthesiser` — LLM expert; reads `{{FaceRecogniser.output}}`, `{{TextExtractor.output}}`,
  `{{ObjectClassifier.output}}` and synthesises a structured annotation

**Note on ONNX:** `FaceRecogniser` and `ObjectClassifier` could be `kind: onnx` embedded
models in the full use case. The demo uses `kind: http` (or LLM stubs) to be runnable
before Phase 22b lands. A comment in the mission file marks where `kind: onnx` would go.

**Demo approach:** Use LLM experts that simulate the ML model outputs (system prompt
describes what a face recogniser would return). This makes the mission runnable today
against any provider. The structural composition is identical to the production form.

**Deliverables:**
- `missions/image-analysis/mission.mcl`
- `missions/image-analysis/forge.toml`
- `missions/image-analysis/experts/{ImageCleaner,FaceRecogniser,TextExtractor,ObjectClassifier,ContextSynthesiser}/expert.md`
- `missions/image-analysis/mcl.lock` (via `forge init`)

---

### Spoke 2 — UC-2: Trading Signal Aggregator

**Source:** `docs/phases/phase-21-parallel-steps.md` → UC-2

**Design:**
```fsharp
mission TradeSignal(ticker) = {
    parallel {
        MarketContext
        StockContext
        SectorContext
    }
    -> SignalSynthesiser
}
```

- `MarketContext` — LLM expert; analyses overall market conditions for the day given `{{ticker}}`
- `StockContext` — LLM expert; analyses the individual stock's recent movement pattern
- `SectorContext` — LLM expert; analyses how the stock's sector is performing
- `SignalSynthesiser` — LLM expert; reads all three by name and produces a structured
  trade signal: BUY / HOLD / SELL with reasoning

Note: The three context experts run concurrently (no dependency on each other) —
this is the canonical fan-out pattern. `SignalSynthesiser` is the fan-in.

**Note on ONNX:** In the production use case, `MarketContext`/`StockContext`/`SectorContext`
could be `kind: onnx` scoring models running against numeric feature vectors (price data,
volume, RSI, etc.). The LLM demo validates the structural composition. ONNX adds the
embedded scoring variant after Phase 22b.

**Note on `kind: http`:** Alternatively, these experts could be `kind: http` calling a
market data API (Alpha Vantage, Polygon, etc.) and returning structured analysis. The
comment in the mission file should note both options.

**Deliverables:**
- `missions/trade-signal/mission.mcl`
- `missions/trade-signal/forge.toml`
- `missions/trade-signal/experts/{MarketContext,StockContext,SectorContext,SignalSynthesiser}/expert.md`
- `missions/trade-signal/mcl.lock` (via `forge init`)

---

### Spoke 3 — UC-3: Log Anomaly Detection

**Source:** `docs/phases/phase-22-non-llm-experts.md` → UC-3

> **Blocked on Phase 22b (ONNX).** The `AnomalyDetector` expert is explicitly an ONNX
> model in the design spec. Until Phase 22b lands, this spoke should not begin.
> A placeholder mission with `kind: http` for the anomaly step can be built as a preview,
> but the canonical form requires `kind: onnx`.

**Design:**
```fsharp
mission LogAnomalyAnalysis(log_stream) = {
    LogParser
    -> AnomalyDetector
    -> RootCauseAnalyst
    -> IncidentReporter
}
```

- `LogParser` — `kind: rule` or LLM; normalises raw log lines into structured context keys
  (`cpu_usage`, `memory_usage`, `request_latency` as numeric values in the context bag)
- `AnomalyDetector` — **`kind: onnx`**; reads `cpu_usage`, `memory_usage`,
  `request_latency` as float features, runs isolation-forest model, writes `anomaly_score`
  to context. Fails (status: fail) if score exceeds threshold.
- `RootCauseAnalyst` — LLM expert; reads `{{output}}` (the flagged log data) and
  `{{anomaly_score}}` from context; produces human-readable root cause hypothesis
- `IncidentReporter` — LLM expert; formats a structured incident summary from the
  root cause analysis

**Expert frontmatter for AnomalyDetector:**
```markdown
---
name: AnomalyDetector
input: Normalised metric features
output: Anomaly score and pass/fail decision
kind: onnx
model: ./models/isolation-forest.onnx
inputs: cpu_usage, memory_usage, request_latency
outputKey: anomaly_score
threshold: 0.85
---
```

**Test model:** For the demo, a minimal ONNX model (e.g., a 3-input linear scoring
model generated with scikit-learn and exported via `skl2onnx`) is sufficient. The demo
doesn't need a production anomaly detector — it needs to prove the pipeline wiring works.

**Deliverables:**
- `missions/log-anomaly/mission.mcl`
- `missions/log-anomaly/forge.toml`
- `missions/log-anomaly/experts/{LogParser,AnomalyDetector,RootCauseAnalyst,IncidentReporter}/expert.md`
- `missions/log-anomaly/models/isolation-forest.onnx` (minimal test model)
- `missions/log-anomaly/mcl.lock` (via `forge init`)

---

## Sequencing

```
Phase 22b Spoke 1 (AOT probe)
    ↓
Phase 22b Spoke 2 (typed context bag)
    ↓
Phase 22b Spoke 3 (frontmatter extension)
    ↓
Phase 22b Spoke 4 (OnnxExpertRunner)         Phase 29 Spoke 1 (UC-1 demo)
    ↓                                              ↓
Phase 22b Spoke 5 (release update)           Phase 29 Spoke 2 (UC-2 demo)
                        ↓
               Phase 29 Spoke 3 (UC-3 demo) ← requires Phase 22b complete
                        ↓
               Phase 27 (Project Assistant / UC-4)
```

UC-1 and UC-2 missions can be built in parallel with Phase 22b once the design is
finalised, since they use LLM/HTTP experts only. UC-3 waits for ONNX.

---

## What each demo must demonstrate

Each demo mission is not just a working example — it is proof that the language can
express the customer's use case clearly and without workarounds. Each mission file should
be readable by a non-MCL user and the thinking model should be obvious from the structure.

The test: show the mission.mcl to someone unfamiliar with MCL. Can they tell what it does?
If yes, the language is expressive enough. If no, we have a design gap.
