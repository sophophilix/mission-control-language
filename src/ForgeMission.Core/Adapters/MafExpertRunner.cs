using ForgeMission.Core.Experts;
using ForgeMission.Core.Runtime;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ForgeMission.Core.Adapters;

/// <summary>
/// IExpertRunner implementation backed by Microsoft Agent Framework.
/// This is the only file in the codebase that touches MAF.
/// </summary>
public class MafExpertRunner(IChatClient chatClient) : IExpertRunner
{
    public async Task<string> RunAsync(
        ExpertDefinition expert,
        Dictionary<string, object> context,
        CancellationToken ct = default)
    {
        var userMessage = context.TryGetValue("output", out var output)
            ? output.ToString()!
            : string.Empty;

        var systemPrompt = ContextInterpolator.Interpolate(expert.SystemPrompt, context);

        var agent   = new ChatClientAgent(chatClient, systemPrompt, expert.Name);
        var session = await agent.CreateSessionAsync(ct);
        var response = await agent.RunAsync(userMessage, session, new ChatClientAgentRunOptions(), ct);
        return response.Text;
    }
}
