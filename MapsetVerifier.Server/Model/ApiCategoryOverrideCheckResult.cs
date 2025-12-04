namespace MapsetVerifier.Server.Model;

public readonly struct ApiCategoryOverrideCheckResult(
    ApiCategoryCheckResult categoryResult,
    Dictionary<int, ApiCheckDefinition> checks)
{
    public ApiCategoryCheckResult CategoryResult { get; } = categoryResult;
    public Dictionary<int, ApiCheckDefinition> Checks { get; } = checks;
}