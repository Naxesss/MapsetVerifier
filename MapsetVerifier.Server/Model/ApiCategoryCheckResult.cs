using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiCategoryCheckResult(
    string category,
    ulong? beatmapId,
    IEnumerable<ApiCheckResult> checkResults,
    Beatmap.Mode? mode,
    Beatmap.Difficulty? difficultyLevel)
{
    public string Category { get; } = category;
    public ulong? BeatmapId { get; } = beatmapId;
    public IEnumerable<ApiCheckResult> CheckResults { get; } = checkResults;
    public Beatmap.Mode? Mode { get; } = mode;
    public Beatmap.Difficulty? DifficultyLevel { get; } = difficultyLevel;
}