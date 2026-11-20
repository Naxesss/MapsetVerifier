using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Mania.Timing;

[Check]
public class CheckManiaScrollSpeed : BeatmapCheck
{
    /// <summary> A section lasting at least this many beats with notes in it is a long-term change. </summary>
    private const double LongTermBeats = 8;

    private static readonly (Difficulty Difficulty, double Min, double Max)[] Ranges =
    [
        (Difficulty.Normal, 0.90, 1.05),
        (Difficulty.Hard, 0.80, 1.10),
        (Difficulty.Insane, 0.70, 1.10),
        (Difficulty.Expert, 0.60, 1.10),
    ];

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Mode.Mania],
            Difficulties =
            [
                Difficulty.Normal,
                Difficulty.Hard,
                Difficulty.Insane,
                Difficulty.Expert,
            ],
            Category = "Timing",
            Message = "Long-term scroll speed outside the guideline range.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out sections of at least 8 beats with notes that scroll faster or slower than the ranking criteria
                    allow for the difficulty:

| Difficulty | Range |
|---|---|
| Normal | 0.90x to 1.05x |
| Hard | 0.80x to 1.10x |
| Insane | 0.70x to 1.10x |
| Expert | 0.60x to 1.10x |"
                },
                {
                    "Reasoning",
                    @"
                    Players set their scroll speed to what they can read. Long sections at a very different speed make them
                    read the map at a speed they didn't pick.

                    The scroll speed is compared to the main BPM of the map, so green lines that normalise other BPMs back to
                    1.0x don't count as changes. Short slider velocity gimmicks are left to your own judgement."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Range",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Scrolls at {1}x for {2} beats, outside {3}x to {4}x.",
                    "timestamp -",
                    "multiplier",
                    "beats",
                    "min",
                    "max"
                )
                    .WithCause(
                        "A long section with notes scrolls faster or slower than the difficulty allows."
                    )
                    .WithRule(
                        RC.Mania.Normal_LongTermSliderVelocityChanges,
                        RC.Mania.Hard_LongTermSliderVelocityChanges,
                        RC.Mania.Insane_LongTermSliderVelocityChanges,
                        RC.Mania.Expert_LongTermSliderVelocityChanges
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        foreach (var (multiplier, beats, objects) in ManiaPatterns.ScrollSections(beatmap))
        {
            if (beats < LongTermBeats || objects.Count == 0)
                continue;

            foreach (var (difficulty, min, max) in Ranges)
            {
                if (multiplier >= min - 0.001 && multiplier <= max + 0.001)
                    continue;

                yield return new Issue(
                    GetTemplate("Range"),
                    beatmap,
                    Timestamp.Get(objects[0]),
                    Format(multiplier),
                    beats.ToString("0", CultureInfo.InvariantCulture),
                    Format(min),
                    Format(max)
                ).ForDifficulties(difficulty);
            }
        }
    }

    private static string Format(double value) =>
        value.ToString("0.0#", CultureInfo.InvariantCulture);
}
