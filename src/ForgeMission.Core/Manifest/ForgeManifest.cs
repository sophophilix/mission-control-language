namespace ForgeMission.Core.Manifest;

public sealed class ForgeManifest
{
    // Expert name → OCI reference (e.g. "ghcr.io/katasec/forge-k8s-architect@0.1.0")
    // Only OCI experts are declared here. Local experts are resolved by name, no declaration needed.
    public IReadOnlyDictionary<string, string> Experts { get; init; }
        = new Dictionary<string, string>();

    // Profile name → provider config. "default" is always required when steps use an LLM.
    public IReadOnlyDictionary<string, ProviderProfile> Providers { get; init; }
        = new Dictionary<string, ProviderProfile>();
}

public sealed class ProviderProfile
{
    public string Provider  { get; init; } = "";
    public string Model     { get; init; } = "";
    public string? ApiKey   { get; init; }
    public string? Endpoint { get; init; }
}
