using MapsetVerifier.Framework.Objects;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiCheckResult(
    int id,
    string message,
    Issue.Level level)
{
    public int Id { get; } = id;
    public string Message { get; } = message;
    public Issue.Level Level { get; } = level;
}