using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiBeatmapStructureDifficulty(
    string category,
    ulong? beatmapId,
    Beatmap.Mode mode,
    double? starRating,
    ManiaPlaystyle? playstyle
)
{
    public string Category { get; } = category;
    public ulong? BeatmapId { get; } = beatmapId;
    public Beatmap.Mode Mode { get; } = mode;
    public double? StarRating { get; } = starRating;
    public ManiaPlaystyle? Playstyle { get; } = playstyle;
}

public readonly struct ApiBeatmapStructure(IEnumerable<ApiBeatmapStructureDifficulty> difficulties)
{
    public IEnumerable<ApiBeatmapStructureDifficulty> Difficulties { get; } = difficulties;
}
