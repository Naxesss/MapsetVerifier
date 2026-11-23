using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Taiko.Timing;

[Check]
public class CheckPatternScrollSpeed : BeatmapCheck
{
    private const double QuarterBeat = 1.0 / 4;

    /// <summary> How many times faster or slower the next note has to scroll for the change to be abrupt. </summary>
    private const double AbruptChange = 1.25;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Taiko],
            Category = "Timing",
            Message = "Abrupt scroll speed changes within overlapping patterns.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out large scroll speed changes in the middle of patterns whose notes already overlap, such as 1/4
                    streams."
                },
                {
                    "Reasoning",
                    @"
                    Notes which overlap are only readable because their spacing is even. If one suddenly moves at a different
                    speed, the spacing breaks up or notes slide over each other. Changing the speed gradually keeps the pattern
                    readable.

                    A change is abrupt if a note scrolls at least 1.25 times faster or slower than the note before it, with BPM
                    taken into account, so slider velocity that only evens out a BPM change is ignored. Patterns are at least 3
                    notes at most 1/4 apart, where beats are scaled for slow and fast songs.

                    The last note of a pattern may change speed, as that commonly emphasises the end of a stream or is where the
                    next section starts."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Abrupt",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Scroll speed abruptly changes to {1}x of what it was, within an overlapping pattern of {2} notes.",
                    "timestamp -",
                    "change",
                    "count"
                )
                    .WithCause(
                        "A note in an overlapping 1/4 pattern, other than the last, scrolls at least 1.25 times faster or slower than the note before it."
                    )
                    .WithRule(RC.Taiko.SliderVelocity_AvoidAbruptSliderVelocityChanges)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        foreach (var pattern in TaikoPatterns.GetPatterns(beatmap, QuarterBeat, minNotes: 3))
        {
            for (var i = 1; i < pattern.Count - 1; i++)
            {
                var previousSpeed = TaikoPatterns.GetScrollSpeed(pattern[i - 1]);
                var speed = TaikoPatterns.GetScrollSpeed(pattern[i]);

                if (previousSpeed <= 0 || speed <= 0)
                    continue;

                var change = speed / previousSpeed;

                if (
                    Math.Max(change, 1 / change) < AbruptChange - Common.ROUNDING_ERROR_MARGIN
                    || !TaikoPatterns.Overlaps(pattern[i - 1], pattern[i], previousSpeed)
                )
                    continue;

                yield return new Issue(
                    GetTemplate("Abrupt"),
                    beatmap,
                    Timestamp.Get(pattern[i - 1], pattern[i]),
                    change.ToString("0.##", CultureInfo.InvariantCulture),
                    pattern.Count
                );
            }
        }
    }
}
