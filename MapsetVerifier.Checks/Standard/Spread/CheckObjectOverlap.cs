using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Spread;

[Check]
public class CheckObjectOverlap : BeatmapCheck
{
    /// <summary> With more overlaps than this, they are reported together instead of one by one. </summary>
    private const int MaxSeparateIssues = 5;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties = [Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal],
            Category = "Spread",
            Message = "Overlapping circles, slider heads and slider tails.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out circles, slider heads and slider tails overlapping each other in Easies and Normals."
                },
                {
                    "Reasoning",
                    @"
                    New players can have a hard time telling overlapping objects apart and seeing which one comes first. If objects
                    1/1 apart overlap because of distance snapping, a higher spacing or slider velocity avoids it.

                    Objects 1/2 apart or less are expected to overlap the object right before them, so those are not reported. Nor
                    are visible stacks, objects which only touch at their borders, or objects which are not on screen at the same time. Other
                    overlaps, such as with an object further back, are only reported if one covers more than half of the
                    other.

                    Ranked maps do this on purpose now and then, so this only points out where to take another look. If it happens
                    often in a difficulty, the overlaps are reported together. Difficulties whose name doesn't say their level are
                    skipped unless a level was chosen for them."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Overlap",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Overlapping circles, slider heads or slider tails.",
                    "timestamp -"
                )
                    .WithCause(
                        "Circles, heads or tails overlap while on screen together, other than visible stacks and objects 1/2 or less after each other."
                    )
                    .WithRule(
                        RC.Osu.Easy_AvoidOverlappingCirclesSliderHeads,
                        RC.Osu.Normal_AvoidOverlappingCirclesSliderHeads
                    )
            },
            {
                "Many",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Objects overlap in {1} places, starting with these.",
                    "timestamps",
                    "count"
                )
                    .WithCause(
                        "Same as the other check, but with more than 5 overlaps in the difficulty."
                    )
                    .WithRule(
                        RC.Osu.Easy_AvoidOverlappingCirclesSliderHeads,
                        RC.Osu.Normal_AvoidOverlappingCirclesSliderHeads
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        if (beatmap.IsDifficultyGuessed())
            yield break;

        var overlaps = OsuOverlaps.EndOverlaps(beatmap).ToList();

        if (overlaps.Count > MaxSeparateIssues)
        {
            var timestamps = overlaps
                .Take(MaxSeparateIssues)
                .Select(overlap => Timestamp.Get(overlap.First, overlap.Second));

            yield return new Issue(
                GetTemplate("Many"),
                beatmap,
                string.Concat(timestamps).TrimEnd(),
                overlaps.Count
            );

            yield break;
        }

        foreach (var overlap in overlaps)
            yield return new Issue(
                GetTemplate("Overlap"),
                beatmap,
                Timestamp.Get(overlap.First, overlap.Second)
            );
    }
}
