# Phase 17 — Provider Configuration

**Status:** Not Started

## Goal

Remove the hardcoded OpenAI provider from the CLI and make the LLM provider fully
configurable through standard `let` bindings in the mission file. Mission authors
declare which provider, key, and model they want — the runtime looks up the default
endpoint for the provider and constructs the right `IChatClient`.

## Design

### Binding convention

Three bindings are required in every mission file. A fourth is optional:

| Binding | Env var (canonical) | Default | Required |
|---------|---------------------|---------|----------|
| `provider` | `MCL_PROVIDER` | `"openai"` | No (defaults to openai) |
| `apiKey` | `MCL_API_KEY` | — | Yes |
| `model` | `MCL_MODEL` | `"gpt-4o-mini"` | No |
| `endpoint` | `MCL_ENDPOINT` | per provider lookup | No |

Standard mission file (3 bindings — OpenAI, no endpoint needed):

```fsharp
let provider = env("MCL_PROVIDER", "openai")
let apiKey   = env("MCL_API_KEY")
let model    = env("MCL_MODEL", "gpt-4o-mini")
```

With endpoint override (Azure, custom proxy, etc.):

```fsharp
let provider = env("MCL_PROVIDER", "azure")
let apiKey   = env("MCL_API_KEY")
let model    = env("MCL_MODEL", "gpt-4o-mini")
let endpoint = env("MCL_ENDPOINT")  // required for azure — no universal default
```

### No grammar changes

`let` bindings and `env()` already express everything needed. This is a runtime
and CLI change only — the grammar and parser are untouched.

### Provider defaults lookup table

The runtime maintains a static map of known providers to their default endpoints:

```csharp
static readonly Dictionary<string, string?> DefaultEndpoints = new(StringComparer.OrdinalIgnoreCase)
{
    ["openai"]    = null,  // OpenAI SDK resolves its own endpoint internally
    ["anthropic"] = "https://api.anthropic.com/v1",
    ["azure"]     = null,  // deployment-specific — MCL_ENDPOINT must be set
};
```

`null` means the SDK handles the endpoint internally. If a provider is not in the
table, the runtime errors with a list of supported providers.

### Endpoint resolution

1. If `endpoint` binding is present and non-empty → use it (overrides everything)
2. Else look up the provider's default in the table
3. If the default is `null` and no override → error: `"Provider 'azure' requires an endpoint: let endpoint = env(\"MCL_ENDPOINT\")"`

### Error messages

| Condition | Error |
|-----------|-------|
| `apiKey` binding absent | `Mission must declare an API key: let apiKey = env("MCL_API_KEY")` |
| `provider` unknown | `Unknown provider 'X'. Supported: openai, anthropic, azure` |
| `provider=azure` and no endpoint | `Provider 'azure' requires an endpoint: let endpoint = env("MCL_ENDPOINT")` |

### Client construction (in `TryBuildRunner`)

Switch on `provider` to construct the right `IChatClient`:

```
"openai"    → new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient()
"azure"     → new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey)).GetChatClient(model).AsIChatClient()
"anthropic" → TBD — depends on MAF Anthropic package availability
```

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Update all mission files to use `MCL_API_KEY` instead of `OPENAI_API_KEY` | Not Started |
| 2 | Add `provider` binding to all mission files | Not Started |
| 3 | Add provider defaults lookup table to CLI | Not Started |
| 4 | Implement endpoint resolution logic in `TryBuildRunner` | Not Started |
| 5 | Implement `IChatClient` factory switching on `provider` | Not Started |
| 6 | Add unit tests for error paths (unknown provider, missing azure endpoint) | Not Started |
| 7 | Update `README.md` Variables section with the four canonical bindings | Not Started |

## Completion Condition

`mcl run` works with `MCL_PROVIDER=openai` (existing behaviour preserved).
All error paths produce the messages specified above.
All existing tests pass. New tests cover unknown provider and missing azure endpoint.

## Notes

- Anthropic support depends on MAF shipping an Anthropic `IChatClient` adapter.
  Task 5 can stub it with a `NotSupportedException` initially.
- The `provider` binding name is reserved by the runtime alongside `apiKey`, `model`,
  and `endpoint`. Document in `docs/design/language.md` under Reserved Context Variables.
