using ForgeMission.Core.Experts;
using ForgeMission.Core.Runtime;

namespace ForgeMission.Tests.Runtime;

public class StubExpertRunner(Func<string, string, string>? respond = null) : IExpertRunner
{
    private readonly Func<string, string, string> _respond = respond ?? ((name, _) => $"Output from {name}");

    public List<(string ExpertName, string Context)> Calls { get; } = [];

    public Task<string> RunAsync(ExpertDefinition expert, string context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Calls.Add((expert.Name, context));
        return Task.FromResult(_respond(expert.Name, context));
    }
}
