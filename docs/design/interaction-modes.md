# Interaction Modes & the Classifier-Router Pattern

## The problem

Current AI agent harnesses treat every human input as a task to execute. They don't.

Real human-AI collaboration has distinct modes — and conflating them degrades the quality of every one:

| Mode | What it looks like |
|------|--------------------|
| **Discovery** | Human needs to understand something — asks questions, seeks explanation, wants the AI to research and synthesise |
| **Design** | Iterative exploration — options compared, decisions made, tradeoffs reasoned through |
| **Planning** | Organising work — hub/spoke docs updated, priorities set, phases sequenced |
| **Execution** | Imperative tasks — build this, document that, commit and push |
| **Research** | External lookup — find current state, check online sources, use findings to inform the next decision |

A developer in a long multi-session effort moves fluidly across all five. An agent harness that only knows how to execute makes the human do all the mode-switching overhead manually — or worse, treats a design question as a task and produces the wrong thing entirely.

## The insight: classifier as router

The fix is a **Classifier expert** at the top of every reasoning chain. Its job is to identify which mode the current input represents and route accordingly — like HAProxy routing HTTP traffic to the right backend based on the request.

```
Human input
     ↓
 Classifier         ← what mode is this?
     ↓
 ┌───┴────────────────────────────┐
 │                                │
Discovery        Design       Execution
  Expert          Expert        Chain
```

This does three things that aren't obvious at first:

1. **Each expert gets clean context.** The Developer never sees the Discovery conversation. The Architect never sees test output. Every expert operates on exactly the context relevant to their role.

2. **The top-level session stays coherent.** Mode-switching happens inside the mission, transparently, without polluting the shared context.

3. **Behaviour becomes codifiable.** Once modes are explicit, you can define exactly how each one should behave — what experts engage, in what order, with what constraints.

## The SDLC meta-mission

A software development lifecycle expressed as a routing mission. The Classifier identifies the mode; conditional execution ensures only the relevant experts engage; the Planner checkpoints after each phase to keep hub/spoke documentation current.

```fsharp
mission SDLCAgent(input) =
    ProductManager                                    // understands intent, frames the problem
    -> RequestClassifier                              // identifies mode: task, design, discovery, research
    -> Planner                                        // updates hub/spoke before any work begins
    -> Architect         when { mode = "design" }     // engages only for design conversations
    -> Planner           when { mode = "design" }     // records design decisions
    -> Developer         when { mode = "task" }       // engages only for execution tasks
    -> Planner           when { mode = "task" }       // records what was built
    -> Tester            when { mode = "task" }       // verifies execution output
    -> Planner           when { mode = "task" }       // records test results
    -> Releaser          when { mode = "task" }       // ships if ready
    -> Planner                                        // final context checkpoint
```

The Planner is woven throughout — not just at the end — because context organisation is not a final step, it is a discipline enforced at every phase boundary.

## Language implication: conditional steps

The `when { }` clause is a new MCL primitive not currently in the grammar. It makes a step conditional on a context bag value set by a prior step:

```fsharp
-> Architect when { mode = "design" }
```

If the condition is false, the step is skipped and the pipeline continues. This is the minimal conditional needed to express routing — not general branching, not match expressions, just step-level guards on context values.

This is a significant but justified addition. It is the language primitive that makes the classifier-router pattern expressible.

## Why this matters beyond SDLC

The classifier-router pattern is not specific to software development. Any complex, multi-session human-AI collaboration has mode boundaries. The pattern is:

```fsharp
mission AnyComplexAgent(input) =
    Classifier
    -> ExpertA when { mode = "a" }
    -> ExpertB when { mode = "b" }
    -> ExpertC when { mode = "c" }
    -> ContextManager
```

The mission codifies not just *how* to reason but *what kind of reasoning is needed* — and routes accordingly. This is the difference between an agent that executes instructions and one that genuinely collaborates.

## Origin

This pattern emerged from direct experience with long multi-session AI-assisted work — extended Grok sessions where context entropy, mode confusion, and lack of structured routing degraded response quality over time. The hub/spoke documentation pattern, the Session Continuity Protocol, and MCL itself all emerged from the same source. The classifier-router is the runtime complement to those structural solutions.

## Open questions

- Should `when { }` evaluate against exact string match, or support richer expressions?
- Who sets `mode` — the Classifier via structured output, or a `StepEnvelope` field?
- Should unmatched `when { }` steps silently skip or emit a trace log entry?
- Is `when { }` a Phase 25 addition or a separate phase given its grammar implications?
