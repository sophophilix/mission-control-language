# MAF — Research Spike

## What it is

Microsoft Agent Framework 1.0 (GA April 2026) is the convergence of Semantic Kernel and AutoGen into a single unified SDK for building agents and multi-agent workflows in .NET and Python.

## What it gives FML for free

| FML need | MAF primitive |
|----------|---------------|
| LLM client abstraction | `ChatClientAgent` — provider-agnostic |
| Context passing between experts | `AgentThread` — carries conversation history between turns |
| Sequential pipeline | `SequentialBuilder` — built-in, first-class pattern |
| Expert as a named reasoning capability | Agent with a system prompt |
| Streaming output | Built-in |
| Checkpointing long pipelines | Built-in |

## Key design decision

`AgentThread` is the context-passing mechanism for the `|>` pipeline. Each expert runs as an agent turn on the same thread — the previous expert's output is naturally available as conversation history to the next. No custom context plumbing needed.

## How MAF is used in FML

MAF is used exclusively inside `MafExpertRunner` — a single file that implements `IExpertRunner`. It does not appear anywhere else in the codebase.

```
FML pipeline runner → IExpertRunner → MafExpertRunner → MAF (ChatClientAgent + AgentThread)
```

## What MAF is not used for

- Parser
- AST
- Expert loading
- Output writing
- CLI

## Packages (1.0)

```xml
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.*" />
<PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.*" />
```

LLM provider is configured at runtime via environment variable. The FML runtime does not hardcode a provider.

## References

- [Microsoft Agent Framework Overview](https://learn.microsoft.com/en-us/agent-framework/overview/)
- [Agent Framework 1.0 GA announcement](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/)
- [Agent Framework Samples](https://github.com/microsoft/Agent-Framework-Samples)
