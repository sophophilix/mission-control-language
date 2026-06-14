# Phase 5 — MAF Adapter

## Goal

Implement `IExpertRunner` using Microsoft Agent Framework. This is the only file in the codebase that touches MAF. Uses `ChatClientAgent` and `AgentThread` to run each expert step against a real LLM.

## Completion condition

Integration test passes: a single expert runs against a real LLM, produces a non-empty structured response, and the output is returned as a string to the pipeline runner.

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Add MAF package references (`Microsoft.Agents.AI`, provider package) | Not Started |
| 2 | Implement `MafExpertRunner` class implementing `IExpertRunner` | Not Started |
| 3 | Configure `ChatClientAgent` with expert's `SystemPrompt` as system message | Not Started |
| 4 | Create `AgentThread` per pipeline run (shared across all steps) | Not Started |
| 5 | Pass incoming context as user message on the thread | Not Started |
| 6 | Extract response text and return as `string` | Not Started |
| 7 | Read LLM provider and API key from environment variables | Not Started |
| 8 | Register `MafExpertRunner` in DI container | Not Started |
| 9 | Integration test: run single expert with real LLM, assert non-empty response | Not Started |
| 10 | Integration test: run two-step pipeline, assert context flows between steps | Not Started |
