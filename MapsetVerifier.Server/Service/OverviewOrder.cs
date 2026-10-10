using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Server.Service;

public static class OverviewOrder
{
    /// <summary>
    /// The mapset's difficulties in the order every Overview analysis returns them: by mode, then
    /// easiest to hardest by star rating, so tables, lists and charts show the spread the same way.
    /// <see cref="BeatmapSet.Beatmaps"/> is ordered for checks instead (by interpreted difficulty
    /// level before star rating), so it isn't used as is.
    /// </summary>
    public static List<Beatmap> InOverviewOrder(this BeatmapSet beatmapSet) =>
        beatmapSet
            .Beatmaps.OrderBy(beatmap => beatmap.GeneralSettings.mode)
            .ThenBy(beatmap => beatmap.StarRating)
            .ThenBy(beatmap => beatmap.MetadataSettings.version, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
