namespace ForgeMission.Core.Runtime;

public record PipelineRunOptions(
    string MissionName,
    string InputText,
    string OutputDirectory,
    IReadOnlyDictionary<string, string>? Vars = null);
