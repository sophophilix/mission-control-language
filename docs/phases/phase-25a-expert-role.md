# Phase 25a — Expert Role Declaration

## Status: Todo

## Motivation

`DirectExpertRunner` injects structured output semantics that include a `status: "fail"` path
into **every** expert's system prompt. This makes sense for judge experts — steps that make a
pass/fail gate decision — but breaks critic experts, which are expected to surface problems and
always continue the pipeline.

When running `elevator-pitch-refined`, `PitchCritic` correctly identified problems with the
draft pitch and returned `status: "fail"`, stopping the mission before `PitchReviser` could
fix those problems. The workaround was adding explicit "always pass" language to the expert's
system prompt — fragile, invisible to mission authors, and easy to forget.

**Principle:** powerful side-effects should require explicit opt-in. The default for any expert
should be safe (always pass), and fail semantics should be declared by the expert that owns them.

## Design

### Frontmatter field

```markdown
---
name: PitchJudge
role: judge
---
```

`role` is optional. Omitted → `critic` (safe default, always pass). Declared as `judge` → runner
injects the full pass/fail structured output instruction.

### Known roles

| Role | Fail semantics | Use for |
|------|----------------|---------|
| (omitted) | Never | Transformers, summarisers, critics, revisers |
| `judge` | On failure, stops the pipeline | Quality gates, reviewers, classifiers that gate flow |

### Runner change

`DirectExpertRunner` reads `expert.Role` from the loaded `ExpertDefinition`. If `Role == "judge"`,
it appends the full StepEnvelope instruction (pass/fail). Otherwise it appends a pass-only wrapper:

```
Respond with this exact JSON format and nothing else:
{"text": "<your complete response>", "status": "pass"}
```

### Expert updates

All existing experts that are not judges (critics, drafters, revisers, analysers) require no change
— the omitted `role` gives them safe behaviour automatically.

Judge experts that should be able to stop the pipeline add `role: judge`:
- `QualityJudge`
- `PitchJudge`
- any future gate expert

### What does not change

- `StepEnvelope` format — `status` field remains
- `PipelineRunner` fail-fast logic — unchanged
- Mission `.mcl` syntax — no grammar change needed
- `when(output: ...)` routing — unaffected

## Implementation

1. Add `Role` property to `ExpertDefinition` (default `"critic"`)
2. Parse `role:` from expert frontmatter in `ExpertLoader`
3. Branch in `DirectExpertRunner.RunAsync` and `StreamAsync` on `expert.Role`
4. Add `role: judge` to `QualityJudge` and `PitchJudge` expert.md files
5. Remove "IMPORTANT: always output status pass" workaround from `PitchCritic`
6. Add unit test: critic expert always produces `status: pass` regardless of LLM intent
