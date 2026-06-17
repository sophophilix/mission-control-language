# Phase 24 — Spoke 2: Copilot SDK Integration Tests

**Status:** Planned  
**Hub:** [phase-24-copilot-sdk-integration-tests.md](phase-24-copilot-sdk-integration-tests.md)  
**Depends on:** Spoke 1 (test harness)  
**Parallel with:** Spoke 3

## Goal

Write two test classes that drive `OaiServer` (wrapping an MCL mission) via the
GitHub Copilot SDK:

1. **Offline test** — `StubExpertRunner` returns a canned response; no network;
   runs in CI unconditionally. Proves the OAI wire protocol and session plumbing.

2. **Live test** — real `DirectExpertRunner` + real LLM via `MCL_API_KEY`;
   skipped if env vars absent. Proves the full round-trip through a real MCL
   pipeline with a real AI coding agent as the client.

---

## Prerequisites (env vars for live tests)

| Var | Purpose |
|-----|---------|
| `MCL_API_KEY` | LLM provider API key |
| `MCL_MODEL` | Model name (default: `gpt-4o-mini`) |
| `MCL_PROVIDER` | Provider (default: `openai`) |
| `COPILOT_GITHUB_TOKEN` | GitHub token with Copilot subscription (for SDK auth) |

Live tests are skipped — not failed — if any of these are absent.

---

## Tasks

### 1. Create `CopilotSdkTests.cs`

File: `src/ForgeMission.Tests/Integration/CopilotSdkTests.cs`

```csharp
using GitHub.Copilot;
using ForgeMission.Core.Adapters;
using ForgeMission.Core.Runtime;
using ForgeMission.Tests.Runtime; // StubExpertRunner

namespace ForgeMission.Tests.Integration;

public sealed class CopilotSdkTests
{
    // ------------------------------------------------------------------
    // Offline: StubExpertRunner returns a canned reply
    // Proves OAI wire protocol + Copilot SDK BYOK plumbing
    // ------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Unit")]
    public async Task CopilotSdk_ReceivesReply_ThroughStubMission()
    {
        // Arrange — stub runner echoes the prompt back
        var stub   = new StubExpertRunner("echo: {{goal}}");
        var client = BuildMissionChatClient(stub);

        await using var fixture = await OaiServerFixture.StartAsync(client);

        await using var copilot = new CopilotClient(new CopilotClientOptions
        {
            UseLoggedInUser = false,
        });
        await copilot.StartAsync();

        var done     = new TaskCompletionSource<string>();
        var session  = await copilot.CreateSessionAsync(new SessionConfig
        {
            Provider = new ProviderConfig
            {
                Type    = "openai",
                BaseUrl = $"{fixture.BaseUrl}/v1",
                ApiKey  = "stub",
            },
            OnPermissionRequest = PermissionHandler.ApproveAll,
        });

        session.On<SessionEvent>(evt =>
        {
            if (evt is AssistantMessageEvent msg)
                done.TrySetResult(msg.Data.Content);
            else if (evt is SessionIdleEvent)
                done.TrySetResult(string.Empty);
        });

        // Act
        await session.SendAsync(new MessageOptions { Prompt = "hello world" });
        var reply = await done.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        Assert.NotEmpty(reply);
    }

    // ------------------------------------------------------------------
    // Live: real LLM through no-op mission
    // Skipped if MCL_API_KEY or COPILOT_GITHUB_TOKEN absent
    // ------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CopilotSdk_LiveRoundTrip_ThroughNoopMission()
    {
        var apiKey       = Environment.GetEnvironmentVariable("MCL_API_KEY");
        var copilotToken = Environment.GetEnvironmentVariable("COPILOT_GITHUB_TOKEN");
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(copilotToken))
            return; // skip

        // Arrange — load no-op mission from test output directory
        var missionDir = Path.Combine(
            AppContext.BaseDirectory, "Missions", "noop");

        var source    = await File.ReadAllTextAsync(Path.Combine(missionDir, "mission.mcl"));
        var ast       = ForgeMission.Parser.MclParser.Parse(source);
        var lockFile  = ForgeMission.Core.Resolution.LockFileIO.Read(
                            Path.Combine(missionDir, "mcl.lock"));
        var experts   = ForgeMission.Core.Experts.ExpertLoader
                            .LoadFromLockFile(lockFile, missionDir);

        var model    = Environment.GetEnvironmentVariable("MCL_MODEL") ?? "gpt-4o-mini";
        var runner   = BuildLiveRunner(apiKey, model);
        var mcClient = new MissionChatClient(ast, experts, runner);

        await using var fixture = await OaiServerFixture.StartAsync(mcClient);

        await using var copilot = new CopilotClient(new CopilotClientOptions
        {
            GitHubToken     = copilotToken,
            UseLoggedInUser = false,
        });
        await copilot.StartAsync();

        var done    = new TaskCompletionSource<string>();
        var session = await copilot.CreateSessionAsync(new SessionConfig
        {
            Provider = new ProviderConfig
            {
                Type    = "openai",
                BaseUrl = $"{fixture.BaseUrl}/v1",
                ApiKey  = "forge",
            },
            OnPermissionRequest = PermissionHandler.ApproveAll,
        });

        session.On<SessionEvent>(evt =>
        {
            if (evt is AssistantMessageEvent msg)
                done.TrySetResult(msg.Data.Content);
            else if (evt is SessionIdleEvent)
                done.TrySetResult(string.Empty);
        });

        // Act
        await session.SendAsync(new MessageOptions { Prompt = "Say exactly: forge works" });
        var reply = await done.Task.WaitAsync(TimeSpan.FromSeconds(60));

        // Assert
        Assert.Contains("forge works", reply, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static MissionChatClient BuildMissionChatClient(IExpertRunner runner)
    {
        var missionDir = Path.Combine(AppContext.BaseDirectory, "Missions", "noop");
        var source     = File.ReadAllText(Path.Combine(missionDir, "mission.mcl"));
        var ast        = ForgeMission.Parser.MclParser.Parse(source);
        var lockFile   = ForgeMission.Core.Resolution.LockFileIO.Read(
                             Path.Combine(missionDir, "mcl.lock"));
        var experts    = ForgeMission.Core.Experts.ExpertLoader
                             .LoadFromLockFile(lockFile, missionDir);
        return new MissionChatClient(ast, experts, runner);
    }

    private static IExpertRunner BuildLiveRunner(string apiKey, string model)
    {
        var chatClient = new OpenAI.OpenAIClient(
                new System.ClientModel.ApiKeyCredential(apiKey))
            .GetChatClient(model)
            .AsIChatClient();
        return new DirectExpertRunner(chatClient);
    }
}
```

---

## Acceptance criteria

- Offline test passes with no network access and no API keys
- Live test is **skipped** (not failed) when env vars are absent
- Live test passes when env vars are present and asserts the expected phrase is
  in the reply
- Both tests appear in `dotnet test --list-tests` output
- CI runs offline test only; live test is opt-in via env vars
