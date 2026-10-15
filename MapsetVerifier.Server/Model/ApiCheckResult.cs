using MapsetVerifier.Framework.Objects;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiCheckResult(
    int id,
    string message,
    Issue.Level level,
    IReadOnlyList<string> ruleIds
)
{
    public int Id { get; } = id;
    public string Message { get; } = message;
    public Issue.Level Level { get; } = level;

    /// <summary> Ranking criteria statements the issue's template enforces. </summary>
    public IReadOnlyList<string> RuleIds { get; } = ruleIds;
}
