namespace MapsetVerifier.Server.Model;

public readonly struct ApiBeatmapSetCheckResult(
    ApiCategoryCheckResult general,
    IEnumerable<ApiCategoryCheckResult> difficulties,
    ulong? beatmapSetId,
    Dictionary<int, ApiCheckDefinition> checks,
    string? title,
    string? artist,
    string? creator)
{
    public ApiCategoryCheckResult General { get; } = general;
    public IEnumerable<ApiCategoryCheckResult> Difficulties { get; } = difficulties;
    public ulong? BeatmapSetId { get; } = beatmapSetId;
    public Dictionary<int, ApiCheckDefinition> Checks { get; } = checks;
    public string? Title { get; } = title;
    public string? Artist { get; } = artist;
    public string? Creator { get; } = creator;
}