namespace ForgeMission.Core.Resolution;

public enum FmsErrorCode
{
    UnknownExpert          = 1,
    DuplicateExpert        = 2,
    CircularReference      = 3,
    MissingFrontmatter     = 4,
    SourceNotFound         = 5,
    StaleLockFile          = 6,
    NotInitialised         = 7,
    OciNotSupported        = 10,
}

public class FmsException(FmsErrorCode code, string message, string? detail = null)
    : Exception($"FMS{(int)code:D3} {message}{(detail is null ? "" : $"\n\n{detail}")}")
{
    public FmsErrorCode Code    { get; } = code;
    public string?      Detail  { get; } = detail;
}
