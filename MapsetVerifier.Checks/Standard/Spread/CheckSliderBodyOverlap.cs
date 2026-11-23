using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Spread;

[Check]
public class CheckSliderBodyOverlap : BeatmapCheck
{
    /// <summary> How many beats each level should keep between sliders that fully overlap. </summary>
    private static readonly Dictionary<Beatmap.Difficulty, (double Beats, string Text)> Limits =
        new()
        {
            [Beatmap.Difficulty.Easy] = (4, "4 beats"),
            [Beatmap.Difficulty.Normal] = (3, "3 beats"),
            [Beatmap.Difficulty.Hard] = (2, "2 beats"),
            [Beatmap.Difficulty.Insane] = (0.5, "half a beat"),
        };

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties =
            [
                Beatmap.Difficulty.Easy,
                Beatmap.Difficulty.Normal,
                Beatmap.Difficulty.Hard,
                Beatmap.Difficulty.Insane,
            ],
            Category = "Spread",
            Message = "Fully overlapped slider bodies.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out sliders whose body is completely hidden under a slider played shortly before, such as a slider
                    going back along the same path."
                },
                {
                    "Reasoning",
                    @"
                    Earlier objects are shown on top of later ones. When the body of a slider is covered by one before it, only
                    its ends can be seen, which makes it look like a circle until the first slider is gone.

                    Easies should keep more than 4 beats between such sliders, Normals 3 beats, Hards 2 beats and Insanes half a
                    beat, counted from the end of the first slider to the start of the second. Beats are measured as if the song
                    were 180 BPM, which is what the ranking criteria are written for.

                    Sliders which are partly visible are not reported, nor are sliders stacked on a slider with the same shape or
                    sliders that only appear after the first one is gone. Difficulties whose name doesn't say their level are
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
                    Issue.Level.Warning,
                    "{0} Slider body is fully covered by a slider ending {1} beats earlier (min {2}).",
                    "timestamp -",
                    "beats",
                    "limit"
                )
                    .WithCause(
                        "The whole body of a slider lies on one that ends at most 4, 3, 2 or half a beat earlier (at 180 BPM), for E/N/H/I respectively."
                    )
                    .WithRule(
                        RC.Osu.Easy_AvoidFullyOverlappingSliderBodies,
                        RC.Osu.Normal_AvoidFullyOverlappingSliderBodies,
                        RC.Osu.Hard_AvoidFullyOverlappingSliderBodies,
                        RC.Osu.Insane_AvoidFullyOverlappingSliderBodies
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        if (beatmap.IsDifficultyGuessed())
            yield break;

        var overlaps = OsuOverlaps
            .BodyOverlaps(beatmap, Limits.Values.Max(limit => limit.Beats))
            .ToList();

        foreach (var (difficulty, limit) in Limits)
        foreach (var overlap in overlaps.Where(overlap => overlap.IsWithin(limit.Beats)))
            yield return new Issue(
                GetTemplate("Overlap"),
                beatmap,
                Timestamp.Get(overlap.Covering, overlap.Covered),
                Math.Max(0, overlap.ReferenceBeats).ToString("0.##", CultureInfo.InvariantCulture),
                limit.Text
            ).ForDifficulties(difficulty);
    }
}
