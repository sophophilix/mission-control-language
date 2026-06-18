# Phase 25 — Spoke 5: Provider Profiles

## Status: Todo

## Overview

Provider configuration moves from `let` bindings in `mission.mcl` to named profiles in `forge.toml`. A provider is a function — `f(provider, model, apiKey, endpoint?)` — and `forge.toml` supplies the arguments. The runtime holds the function implementations.

## `forge.toml` profiles

```toml
[providers.default]
provider = "openai"
model    = "gpt-4o-mini"
apiKey   = env("MCL_API_KEY")

[providers.architect]
provider = "anthropic"
model    = "claude-opus-4-8"
apiKey   = env("ANTHROPIC_API_KEY")

[providers.local]
provider = "ollama"
model    = "llama3"
endpoint = "http://localhost:11434"
```

## Per-step override in `mission.mcl`

```fsharp
mission BuildOperatorDesign(goal, persona) =
    KubernetesArchitect with { provider = "architect" }
    -> SecurityArchitect
    -> PrincipalReviewer with { style = "terse ADR" }
```

`with { provider = "architect" }` tells the runtime to look up the `architect` profile in `forge.toml` and use it for that step only. All other steps use `default`.

## Resolution

At step execution time:

1. Check `with { }` clause for `provider` key
2. If present — look up named profile in `ForgeManifest.Providers`
3. If absent — use `providers.default`
4. Construct `IChatClient` from the resolved profile
5. Execute the step

## Supported provider types

| `provider` value | Required fields | Notes |
|-----------------|-----------------|-------|
| `openai` | `apiKey`, `model` | Default endpoint is `api.openai.com` |
| `anthropic` | `apiKey`, `model` | |
| `azure` | `apiKey`, `model`, `endpoint` | `endpoint` is mandatory |
| `ollama` | `model`, `endpoint` | No `apiKey` required |

Unknown `provider` values are an error at startup — not at step execution time.

## What changes from current behaviour

Currently `provider`, `apiKey`, `model`, `endpoint` are `let` bindings in `mission.mcl` read by the runtime from the context bag. That mechanism is replaced by `forge.toml` profiles.

The `let` bindings for domain variables (`goal`, `persona`, etc.) are unchanged — only the four reserved infrastructure bindings (`provider`, `apiKey`, `model`, `endpoint`) are removed from `.mcl`.

## Migration of existing missions

Existing missions with `let provider = ...` bindings continue to work during a transition period — the runtime falls back to context bag resolution if no `forge.toml` is present. A deprecation warning is emitted. Full removal in a subsequent phase.
