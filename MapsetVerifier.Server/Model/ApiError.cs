namespace MapsetVerifier.Server.Model;

public readonly struct ApiError(string message, string? details, string? stackTrace = null)
{
    public string Message { get; } = message;
    public string? Details { get; } = details;
    public string? StackTrace { get; } = stackTrace;
}
