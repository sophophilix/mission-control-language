using ForgeMission.Core.Experts;

namespace ForgeMission.Core.Runtime;

public interface IExpertRunner
{
    Task<string> RunAsync(
        ExpertDefinition expert,
        Dictionary<string, object> context,
        CancellationToken ct = default);
}
