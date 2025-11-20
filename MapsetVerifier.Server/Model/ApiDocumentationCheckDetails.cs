using MapsetVerifier.Framework.Objects;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiDocumentationCheckDetails(string description, List<ApiDocumentationCheckDetailsOutcome> outcomes)
{
    public string Description { get; } = description;
    public List<ApiDocumentationCheckDetailsOutcome> Outcomes { get; } = outcomes;
}

public readonly struct ApiDocumentationCheckDetailsOutcome(Issue.Level level, string description, string? cause)
{
    public Issue.Level Level { get; } = level;
    public string Description { get; } = description;
    public string? Cause { get; } = cause;
}