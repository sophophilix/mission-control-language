# Phase 24 — Copilot SDK Integration Tests (HUB)

**Status:** Design  
**Depends on:** Phase 19 (Agent Runtime — `forge serve`, `OaiServer`), Phase 23 (Container Commands)

## Goal

Prove that a real AI coding agent can drive through an MCL mission end-to-end by
writing a C# integration test that uses the GitHub Copilot SDK as the client,
starts `OaiServer` in-process on a random port, and asserts the response flows
back correctly. Once the pattern is proven with Copilot SDK, a second test target
repeats it with Claude Code (the most widely-used SWE agent) via process/stdio.

This gives us a repeatable, automated way to verify that MCL missions improve (or
at least do not degrade) the agent experience for real users.

---

## Context: why this matters

`forge serve` exposes an OAI-compatible endpoint. Any AI coding agent that supports
a custom `baseUrl` or `ANTHROPIC_BASE_URL` / `OPENAI_BASE_URL` env override can be
pointed at forge. MCL missions then sit in the middle — intercepting the agent's
requests, running them through a pipeline of experts, and returning the result.

A no-op mission (single pass-through expert) proves the plumbing. From there,
real missions can improve responses (summarisation, guardrails, context injection)
without the agent noticing the seam.

---

## Architecture

```
Test process
  │
  ├─ OaiServer (in-process, random TCP port)
  │    └─ MissionChatClient → PipelineRunner → IExpertRunner (stub or real)
  │
  └─ CopilotClient (GitHub.Copilot.SDK)
       └─ BYOK ProviderConfig { Type="openai", BaseUrl="http://localhost:{port}/v1" }
            └─ Copilot CLI subprocess → HTTP → OaiServer
```

The Copilot SDK spawns the Copilot CLI as a subprocess and communicates over
JSON-RPC/stdio. The CLI makes real HTTP calls to `BaseUrl`. This means:

- `OaiServer` must bind a real TCP port (no in-memory `TestServer` — the CLI
  subprocess cannot share an in-memory transport with the test process)
- Port 0 is used so the OS assigns a free port; the actual port is read after bind

---

## Decisions (final — do not re-litigate in spokes)

### Port allocation

Use `TcpListener` with port `0` to find a free port, release the listener, then
pass that port to `OaiServer.Build`. There is a small TOCTOU window; acceptable
for tests. Do not hardcode a port.

### Session store for tests

Use an `InMemorySessionStore` (new class) rather than `LocalFileSessionStore` so
tests leave no files on disk and can run in parallel without collision.

### No-op mission

A minimal `.mcl` file with a single expert whose system prompt is
`"You are a pass-through. Return the user's message unchanged."` The expert is
backed by a real LLM call (using `MCL_API_KEY` / `MCL_MODEL` from the environment)
so the test validates the full round-trip including LLM inference.

Tests that must not call the real LLM use `StubExpertRunner` (already exists in
`ForgeMission.Tests`) so they run offline and fast.

### Test categories

Two categories via `[Trait]`:
- `"Category", "Unit"` — stub runner, no network, always run in CI
- `"Category", "Integration"` — real LLM + real Copilot CLI, skipped if
  `MCL_API_KEY` or `COPILOT_GITHUB_TOKEN` is absent

### Claude Code target (Spoke 3)

Claude Code is driven via `Process.Start("claude", "-p {prompt} --output-format json")` 
with `ANTHROPIC_BASE_URL=http://localhost:{port}/v1` in the process environment.
Stdout is parsed as JSON to extract the response text.
This spoke is written after Spokes 1–2 prove the pattern.

---

## File layout (new files only)

```
src/ForgeMission.Tests/
  Integration/
    OaiServerFixture.cs          # starts OaiServer on a random port, IAsyncDisposable
    InMemorySessionStore.cs      # ISessionStore backed by ConcurrentDictionary
    CopilotSdkTests.cs           # Spoke 1 — Copilot SDK test class
    ClaudeCodeTests.cs           # Spoke 3 — Claude Code process test class
  Missions/
    noop/
      mission.mcl                # minimal pass-through mission
      experts/
        PassThrough/
          expert.md              # pass-through expert
      mcl.lock                   # pre-generated; checked in
```

---

## Spokes

| Spoke | What it builds | File |
|-------|----------------|------|
| 1 | `OaiServerFixture` + `InMemorySessionStore` + no-op mission | [spoke-1](phase-24-spoke-1-test-harness.md) |
| 2 | Copilot SDK integration tests (stub + real LLM) | [spoke-2](phase-24-spoke-2-copilot-sdk-tests.md) |
| 3 | Claude Code process integration tests | [spoke-3](phase-24-spoke-3-claude-code-tests.md) |

Spoke 2 depends on Spoke 1. Spoke 3 depends on Spoke 1.
Spokes 2 and 3 can run in parallel once Spoke 1 is done.
