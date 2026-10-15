using MapsetVerifier.Framework.Objects;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiCheckRunDelta(
    DateTime previousRunAt,
    DateTime currentRunAt,
    IEnumerable<ApiCheckDeltaIssue> newIssues,
    IEnumerable<ApiCheckDeltaIssue> resolvedIssues,
    IEnumerable<ApiCheckDeltaIssue> worsenedIssues,
    IEnumerable<ApiCheckDeltaIssue> improvedIssues,
    IEnumerable<ApiCheckDeltaIssue> unchangedIssues
)
{
    public DateTime PreviousRunAt { get; } = previousRunAt;
    public DateTime CurrentRunAt { get; } = currentRunAt;
    public IEnumerable<ApiCheckDeltaIssue> NewIssues { get; } = newIssues;
    public IEnumerable<ApiCheckDeltaIssue> ResolvedIssues { get; } = resolvedIssues;
    public IEnumerable<ApiCheckDeltaIssue> WorsenedIssues { get; } = worsenedIssues;
    public IEnumerable<ApiCheckDeltaIssue> ImprovedIssues { get; } = improvedIssues;
    public IEnumerable<ApiCheckDeltaIssue> UnchangedIssues { get; } = unchangedIssues;
}

public readonly struct ApiCheckDeltaIssue(
    string category,
    int id,
    string checkName,
    string message,
    Issue.Level level,
    Issue.Level? previousLevel
)
{
    public string Category { get; } = category;
    public int Id { get; } = id;
    public string CheckName { get; } = checkName;
    public string Message { get; } = message;
    public Issue.Level Level { get; } = level;
    public Issue.Level? PreviousLevel { get; } = previousLevel;
}
