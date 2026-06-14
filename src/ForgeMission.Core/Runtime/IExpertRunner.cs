using ForgeMission.Core.Experts;

namespace ForgeMission.Core.Runtime;

public interface IExpertRunner
{
    Task<string> RunAsync(ExpertDefinition expert, string context, CancellationToken ct = default);
}
