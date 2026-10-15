using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.RankingCriteria;

/// <summary> A ranking criteria page from the osu-wiki which is part of the snapshot. </summary>
/// <param name="Key"> Short key used for file names, rule ids and routes, e.g. "osu". </param>
/// <param name="Title"> Human readable title. </param>
/// <param name="WikiPath"> Path of the page within the osu-wiki, relative to <c>wiki/</c>. </param>
/// <param name="ConstantsClass"> Name of the nested class holding this page's rule id constants. </param>
/// <param name="Modes"> The game modes the statements on this page apply to. </param>
/// <param name="HasStatements"> Whether rules and guidelines are parsed from this page, or it is only kept for viewing. </param>
public sealed record RcPage(
    string Key,
    string Title,
    string WikiPath,
    string ConstantsClass,
    Beatmap.Mode[] Modes,
    bool HasStatements = true
)
{
    public string WikiUrl => "https://osu.ppy.sh/wiki/en/" + WikiPath;
    public string SourcePath => "wiki/" + WikiPath + "/en.md";
}

public static class RcPages
{
    private static readonly Beatmap.Mode[] AllModes =
    [
        Beatmap.Mode.Standard,
        Beatmap.Mode.Taiko,
        Beatmap.Mode.Catch,
        Beatmap.Mode.Mania,
    ];

    /// <summary>
    ///     The pages of the ranking criteria which are snapshotted. Simplified_ranking_criteria (a summary of these pages),
    ///     Ranking_Criteria_Council (historical) and Skin_set_list (a reference list) are intentionally left out.
    /// </summary>
    public static readonly IReadOnlyList<RcPage> All =
    [
        new("general", "Ranking criteria", "Ranking_criteria", "General", AllModes),
        new("osu", "osu!", "Ranking_criteria/osu!", "Osu", [Beatmap.Mode.Standard]),
        new("taiko", "osu!taiko", "Ranking_criteria/osu!taiko", "Taiko", [Beatmap.Mode.Taiko]),
        new("catch", "osu!catch", "Ranking_criteria/osu!catch", "Catch", [Beatmap.Mode.Catch]),
        new("mania", "osu!mania", "Ranking_criteria/osu!mania", "Mania", [Beatmap.Mode.Mania]),
        new("metadata", "Metadata", "Ranking_criteria/Metadata", "Metadata", AllModes),
        new(
            "scaling-bpm",
            "Scaling BPM",
            "Ranking_criteria/Scaling_BPM",
            "ScalingBpm",
            AllModes,
            false
        ),
        new(
            "difficulty-naming",
            "Difficulty naming",
            "Ranking_criteria/Difficulty_naming",
            "DifficultyNaming",
            AllModes,
            false
        ),
    ];

    public static RcPage? Get(string key) => All.FirstOrDefault(page => page.Key == key);

    /// <summary> Finds a page by its osu-wiki path, e.g. "Ranking_criteria/osu!". Case-insensitive. </summary>
    public static RcPage? GetByWikiPath(string wikiPath) =>
        All.FirstOrDefault(page =>
            string.Equals(page.WikiPath, wikiPath.Trim('/'), StringComparison.OrdinalIgnoreCase)
        );
}
