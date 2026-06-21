namespace ForgeMission.Core.Experts;

public record ExpertDefinition(
    string Name,
    string Input,
    string Output,
    string SystemPrompt,
    string Role     = "",
    string Kind     = "llm",
    string Endpoint = "")
{
    public bool IsJudge => Role.Equals("judge", StringComparison.OrdinalIgnoreCase);
    public bool IsHttp  => Kind.Equals("http",  StringComparison.OrdinalIgnoreCase);
}
