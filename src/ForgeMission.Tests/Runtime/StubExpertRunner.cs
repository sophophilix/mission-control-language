using ForgeMission.Core.Experts;
using ForgeMission.Core.Runtime;

namespace ForgeMission.Tests.Runtime;

public class StubExpertRunner(
    Func<string, Dictionary<string, object>, string>? respond = null) : IExpertRunner
{
    private readonly Func<string, Dictionary<string, object>, string> _respond
        = respond ?? ((name, _) => $"Output from {name}");

    public List<(string ExpertName, Dictionary<string, object> Context)> Calls { get; } = [];

    public Task<string> RunAsync(
        ExpertDefinition expert,
        Dictionary<string, object> context,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Calls.Add((expert.Name, new Dictionary<string, object>(context, StringComparer.Ordinal)));
        return Task.FromResult(_respond(expert.Name, context));
    }
}
