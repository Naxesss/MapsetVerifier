namespace MapsetVerifier.Server.Model;

public readonly struct ApiBeatmapSetCheckResult(
    ApiCategoryCheckResult general,
    IEnumerable<ApiCategoryCheckResult> difficulties,
    Dictionary<int, ApiCheckDefinition> checks)
{
    public ApiCategoryCheckResult General { get; } = general;
    public IEnumerable<ApiCategoryCheckResult> Difficulties { get; } = difficulties;
    public Dictionary<int, ApiCheckDefinition> Checks { get; } = checks;
}