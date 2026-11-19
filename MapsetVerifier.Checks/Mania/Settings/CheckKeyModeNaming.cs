using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Mania.Settings;

[Check]
public class CheckKeyModeNaming : BeatmapSetCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Mania],
            Category = "Settings",
            Message = "Key mode in difficulty names.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Making sure difficulty names show their key mode or playstyle only when a set needs it.

                    A set needs it when it has more than one key mode or playstyle, such as 4K and 7K, or a key count above 10.
                    Then every difficulty name has to show its own, like ""[7K] Hard"". Otherwise no difficulty name should show one."
                },
                {
                    "Reasoning",
                    @"
                    In sets with several key modes, the names are the only way to tell difficulties apart before playing them.
                    In sets with only one key mode, the marker adds nothing, and the key count is already shown in-game."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Missing",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "\"{0}\" should show its key mode, {1}, because {2}.",
                    "difficulty name",
                    "key mode",
                    "reason"
                )
                    .WithCause(
                        "The set has several key modes or a key count above 10, but a difficulty name doesn't show its key mode."
                    )
                    .WithRule(RC.Mania.BeatmapIncludesKeyCountsAbove)
            },
            {
                "Wrong",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "\"{0}\" shows {1}, but is played in {2}.",
                    "difficulty name",
                    "marker",
                    "key mode"
                )
                    .WithCause(
                        "A difficulty name shows a different key mode than the difficulty is played in."
                    )
                    .WithRule(RC.Mania.BeatmapIncludesKeyCountsAbove)
            },
            {
                "Unneeded",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "\"{0}\" shows its key mode, {1}, while every difficulty is {2}.",
                    "difficulty name",
                    "marker",
                    "key mode"
                )
                    .WithCause(
                        "The set has a single key mode of 10 keys or less, but a difficulty name shows its key mode."
                    )
                    .WithRule(RC.Mania.BeatmapIncludesKeyCountsAbove)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        var difficulties = beatmapSet
            .Beatmaps.Select(beatmap => (Beatmap: beatmap, Playstyle: ManiaPlaystyle.Of(beatmap)))
            .Where(pair => pair.Playstyle != null)
            .Select(pair => (pair.Beatmap, Playstyle: pair.Playstyle!))
            .ToList();

        if (difficulties.Count == 0)
            yield break;

        var labels = difficulties.Select(pair => pair.Playstyle.Label).Distinct().ToList();
        var aboveTen = difficulties.Any(pair => pair.Playstyle.Keys > 10);
        var needsMarker = aboveTen || labels.Count > 1;
        var reason =
            labels.Count > 1
                ? $"the set has more than one key mode ({string.Join(", ", labels)})"
                : "the set has a key count above 10";

        foreach (var (beatmap, playstyle) in difficulties)
        {
            var name = beatmap.MetadataSettings.version;

            if (playstyle.Marker != null && !playstyle.Denoted)
                yield return new Issue(
                    GetTemplate("Wrong"),
                    beatmap,
                    name,
                    playstyle.Marker,
                    playstyle.Label
                );
            else if (needsMarker && playstyle.Marker == null)
                yield return new Issue(
                    GetTemplate("Missing"),
                    beatmap,
                    name,
                    playstyle.Label,
                    reason
                );
            else if (!needsMarker && playstyle.Marker != null)
                yield return new Issue(
                    GetTemplate("Unneeded"),
                    beatmap,
                    name,
                    playstyle.Marker,
                    playstyle.Label
                );
        }
    }
}
