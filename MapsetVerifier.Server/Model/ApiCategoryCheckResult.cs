using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiCategoryCheckResult(
    string category,
    ulong? beatmapId,
    IEnumerable<ApiCheckResult> checkResults,
    Beatmap.Mode? mode,
    Beatmap.Difficulty? difficultyLevel,
    double? starRating,
    ManiaPlaystyle? playstyle = null
)
{
    public string Category { get; } = category;
    public ulong? BeatmapId { get; } = beatmapId;
    public IEnumerable<ApiCheckResult> CheckResults { get; } = checkResults;
    public Beatmap.Mode? Mode { get; } = mode;
    public Beatmap.Difficulty? DifficultyLevel { get; } = difficultyLevel;
    public double? StarRating { get; } = starRating;
    public ManiaPlaystyle? Playstyle { get; } = playstyle;
}
