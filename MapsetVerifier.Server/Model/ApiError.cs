namespace MapsetVerifier.Server.Model;

public readonly struct ApiError(string message, string? details)
{
    public string Message { get; } = message;
    public string? Details { get; } = details;
}
