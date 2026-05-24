using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiBeatmapStructureDifficulty(
    string category,
    ulong? beatmapId,
    Beatmap.Mode mode,
    double? starRating
)
{
    public string Category { get; } = category;
    public ulong? BeatmapId { get; } = beatmapId;
    public Beatmap.Mode Mode { get; } = mode;
    public double? StarRating { get; } = starRating;
}

public readonly struct ApiBeatmapStructure(IEnumerable<ApiBeatmapStructureDifficulty> difficulties)
{
    public IEnumerable<ApiBeatmapStructureDifficulty> Difficulties { get; } = difficulties;
}
