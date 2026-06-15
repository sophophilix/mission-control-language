using System.Runtime.CompilerServices;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Appended to system prompt only for streaming calls — instructs the LLM to emit JSON
    // that ParseStreamedEnvelope can deserialise into StepEnvelope.
    private const string StreamingJsonInstruction = """


Respond with this exact JSON format and nothing else:
{"text": "<your complete response>", "status": "pass"}
Or on failure:
{"text": "<brief summary>", "status": "fail", "reason": "<which criterion failed>"}
""";

    public async Task<StepEnvelope> RunAsync(
        ExpertDefinition expert,
        Dictionary<string, object> context,
        CancellationToken ct = default)
    {
        var (userMessage, systemPrompt) = BuildMessages(expert, context);
        var agent    = new ChatClientAgent(chatClient, systemPrompt, expert.Name);
        var session  = await agent.CreateSessionAsync(ct);
        var response = await agent.RunAsync<StepEnvelope>(userMessage, session, _jsonOptions, new ChatClientAgentRunOptions(), ct);
        return response.Result;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ExpertDefinition expert,
        Dictionary<string, object> context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (userMessage, systemPrompt) = BuildMessages(expert, context);
        var agent   = new ChatClientAgent(chatClient, systemPrompt + StreamingJsonInstruction, expert.Name);
        var session = await agent.CreateSessionAsync(ct);

        await foreach (var update in agent.RunStreamingAsync(userMessage, session, new ChatClientAgentRunOptions(), ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }

    private static (string userMessage, string systemPrompt) BuildMessages(
        ExpertDefinition expert,
        Dictionary<string, object> context)
    {
        var userMessage  = context.TryGetValue("output", out var output) && !string.IsNullOrWhiteSpace(output?.ToString())
            ? output.ToString()!
            : "Begin.";
        var systemPrompt = ContextInterpolator.Interpolate(expert.SystemPrompt, context);
        return (userMessage, systemPrompt);
    }
}
