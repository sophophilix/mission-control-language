using ForgeMission.Core.Adapters;
using ForgeMission.Core.Experts;
using ForgeMission.Core.Resolution;
using ForgeMission.Core.Runtime;
using ForgeMission.Parser;
using ForgeMission.Tests.Runtime;
using GitHub.Copilot;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Collections.Concurrent;

namespace ForgeMission.Tests.Integration;

public sealed class CopilotSdkTests
{
    // -------------------------------------------------------------------------
    // Offline: StubExpertRunner returns a canned reply.
    // Proves OAI wire protocol + Copilot SDK BYOK plumbing without any network.
    // -------------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Unit")]
    public async Task CopilotSdk_ReceivesReply_ThroughStubMission()
    {
        var stub      = new StubExpertRunner((_, _) => "stub reply");
        var mcClient  = BuildMissionChatClient(stub);

        await using var fixture = await OaiServerFixture.StartAsync(mcClient);

        await using var copilot = new CopilotClient(new CopilotClientOptions
        {
            UseLoggedInUser = false,
        });
        await copilot.StartAsync();

        await using var session = await copilot.CreateSessionAsync(new SessionConfig
        {
            Model = "gpt-4o-mini",
            Provider = new ProviderConfig
            {
                Type    = "openai",
                BaseUrl = $"{fixture.BaseUrl}/v1",
                ApiKey  = "stub",
            },
            OnPermissionRequest = PermissionHandler.ApproveAll,
        });

        var reply = await session.SendAndWaitAsync(new MessageOptions { Prompt = "hello" });

        Assert.NotNull(reply);
        Assert.NotEmpty(reply.Data.Content);
    }

    // -------------------------------------------------------------------------
    // Live: real LLM through no-op mission via Copilot SDK BYOK.
    // Skipped when MCL_API_KEY or COPILOT_GITHUB_TOKEN is absent.
    // -------------------------------------------------------------------------
    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task CopilotSdk_LiveRoundTrip_ThroughNoopMission()
    {
        var apiKey       = Environment.GetEnvironmentVariable("MCL_API_KEY");
        var copilotToken = Environment.GetEnvironmentVariable("COPILOT_GITHUB_TOKEN");
        Skip.If(string.IsNullOrWhiteSpace(apiKey), "MCL_API_KEY not set");

        var model    = Environment.GetEnvironmentVariable("MCL_MODEL") ?? "gpt-4o-mini";
        var runner   = BuildLiveRunner(apiKey!, model);
        var mcClient = BuildMissionChatClient(runner);

        await using var fixture = await OaiServerFixture.StartAsync(mcClient);

        // Use a temp dir so the CLI subprocess ignores any cached keyring tokens
        var tmpHome = Path.Combine(Path.GetTempPath(), $"copilot-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpHome);

        await using var copilot = new CopilotClient(new CopilotClientOptions
        {
            GitHubToken   = copilotToken,
            BaseDirectory = tmpHome,
        });
        await copilot.StartAsync();

        var allContent = new ConcurrentBag<string>();

        await using var session = await copilot.CreateSessionAsync(new SessionConfig
        {
            Provider = new ProviderConfig
            {
                Type    = "openai",
                BaseUrl = $"{fixture.BaseUrl}/v1",
                ApiKey  = "forge",
                ModelId = "gpt-4o-mini",
            },
            OnPermissionRequest = PermissionHandler.ApproveAll,
            OnEvent = evt =>
            {
                if (evt is AssistantMessageEvent am && !string.IsNullOrEmpty(am.Data?.Content))
                    allContent.Add(am.Data.Content);
            },
        });

        var reply = await session.SendAndWaitAsync(
            new MessageOptions { Prompt = "Say exactly: forge works" },
            cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);

        // Collect all assistant messages across the agent's turns
        var combined = string.Join("\n", allContent);
        Assert.True(
            combined.Contains("forge works", StringComparison.OrdinalIgnoreCase)
            || (reply?.Data?.Content?.Length > 0),
            $"Expected non-empty response or 'forge works'. Got: {combined}. Final reply: {reply?.Data?.Content}");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static MissionChatClient BuildMissionChatClient(IExpertRunner runner)
    {
        var missionDir = Path.Combine(AppContext.BaseDirectory, "Missions", "noop");
        var source     = File.ReadAllText(Path.Combine(missionDir, "mission.mcl"));
        var ast        = MclParser.Parse(source);
        var lockFile   = LockFileIO.Read(Path.Combine(missionDir, "mcl.lock"));
        var experts    = ExpertLoader.LoadFromLockFile(lockFile, missionDir);
        return new MissionChatClient(ast, experts, runner);
    }

    private static IExpertRunner BuildLiveRunner(string apiKey, string model)
    {
        var chatClient = new OpenAIClient(new ApiKeyCredential(apiKey))
            .GetChatClient(model)
            .AsIChatClient();
        return new DirectExpertRunner(chatClient);
    }
}
