using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Taiko.Compose;

[Check]
public class CheckPatternColours : BeatmapCheck
{
    private const double QuarterBeat = 1.0 / 4;

    /// <summary> A Muzukashii may have this many 1/4 patterns with colour changes, whatever their share is. </summary>
    private const int AllowedColouredPatterns = 10;

    /// <summary> A Muzukashii may have colour changes in this share of its 1/4 patterns, whatever their number is. </summary>
    private const double AllowedColouredShare = 0.25;

    /// <summary> 1/4 patterns with colour changes should be further apart than this many beats in a Muzukashii. </summary>
    private const double ConjunctionBeats = 1;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Mode.Taiko],
            Difficulties = [Difficulty.Hard, Difficulty.Insane],
            Category = "Compose",
            Message = "Complex colour changes in 1/4 patterns.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out 1/4 patterns whose colours are too complex for Muzukashiis and Onis.

| Difficulty | Pointed out |
|---|---|
| Muzukashii | patterns longer than 3 notes with more than one colour change, or a colour change that isn't at the start or end |
| Muzukashii | patterns with colour changes at most one beat after each other |
| Muzukashii | more than 10 patterns with colour changes, if those are more than a quarter of all 1/4 patterns |
| Oni | patterns longer than 5 notes with more colour changes than half the number of notes |"
                },
                {
                    "Reasoning",
                    @"
                    Every colour change in a fast pattern means switching between dons and kats without time to think. Players
                    of these difficulties are still learning to do so, and can only read and play simple combinations.

                    Patterns are notes at most 1/4 apart, where beats are scaled for slow and fast songs, so that 1/2 at 300 BPM
                    counts as 1/4. Drumrolls and spinners end a pattern.

                    In a Muzukashii, kdddd and ddddk change colour at the start and the end, while ddkkk and kdddk are too complex.

                    The ranking criteria don't say how much is too much for the last three rows of the table, so these limits are
                    based on ranked difficulties and are minor issues. Difficulties whose name doesn't say their level are
                    skipped, unless a level is chosen for them."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Muzukashii",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} 1/4 pattern of {1} notes ({2}) should have at most one colour change, at the start or end.",
                    "timestamp -",
                    "count",
                    "colours"
                )
                    .WithCause(
                        "A 1/4 pattern longer than 3 notes has multiple colour changes, or one in the middle, in a Muzukashii."
                    )
                    .WithRule(RC.Taiko.Muzukashii_14PatternsLongerThree)
            },
            {
                "Conjunction",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} 1/4 patterns with colour changes ({1} and {2}) right after each other.",
                    "timestamp -",
                    "colours",
                    "colours"
                )
                    .WithCause(
                        "Two 1/4 patterns with colour changes are at most one beat apart in a Muzukashii."
                    )
                    .WithRule(RC.Taiko.Muzukashii_14PatternsOneMore)
            },
            {
                "Sparingly",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} of {1} 1/4 patterns have colour changes, these should be used sparingly.",
                    "count",
                    "total"
                )
                    .WithCause(
                        "More than 10 of the 1/4 patterns in a Muzukashii, and more than a quarter of them, have colour changes."
                    )
                    .WithRule(RC.Taiko.Muzukashii_14PatternsOneMore)
            },
            {
                "Oni",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} 1/4 pattern of {1} notes ({2}) has {3} colour changes.",
                    "timestamp -",
                    "count",
                    "colours",
                    "changes"
                )
                    .WithCause(
                        "A 1/4 pattern longer than 5 notes has more colour changes than half its number of notes in an Oni."
                    )
                    .WithRule(RC.Taiko.Oni_14PatternsLongerFive)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        // Star rating alone can't tell a Muzukashii from an Oni well enough to judge their patterns.
        if (beatmap.IsDifficultyGuessed())
            yield break;

        var patterns = TaikoPatterns.GetPatterns(beatmap, QuarterBeat, minNotes: 2).ToList();
        var coloured = patterns
            .Where(pattern => TaikoPatterns.GetColourGroups(pattern).Count > 1)
            .ToList();

        foreach (var pattern in coloured)
        {
            var groups = TaikoPatterns.GetColourGroups(pattern);
            var changes = groups.Count - 1;

            if (pattern.Count > 3 && (changes > 1 || (groups[0] > 1 && groups[^1] > 1)))
                yield return new Issue(
                    GetTemplate("Muzukashii"),
                    beatmap,
                    Timestamp.Get(pattern[0], pattern[^1]),
                    pattern.Count,
                    TaikoPatterns.GetColours(pattern)
                ).ForDifficulties(Difficulty.Hard);

            if (pattern.Count > 5 && changes * 2 > pattern.Count)
                yield return new Issue(
                    GetTemplate("Oni"),
                    beatmap,
                    Timestamp.Get(pattern[0], pattern[^1]),
                    pattern.Count,
                    TaikoPatterns.GetColours(pattern),
                    changes
                ).ForDifficulties(Difficulty.Insane);
        }

        for (var i = 1; i < coloured.Count; i++)
        {
            var previousEnd = coloured[i - 1][^1];
            var start = coloured[i][0];
            var line = beatmap.GetTimingLine<UninheritedLine>(previousEnd.time);

            if (
                line == null
                || start.time - previousEnd.time
                    > line.GetNormalizedMsPerBeat() * ConjunctionBeats + Common.MS_EPSILON
            )
                continue;

            yield return new Issue(
                GetTemplate("Conjunction"),
                beatmap,
                Timestamp.Get(coloured[i - 1][0], coloured[i][^1]),
                TaikoPatterns.GetColours(coloured[i - 1]),
                TaikoPatterns.GetColours(coloured[i])
            ).ForDifficulties(Difficulty.Hard);
        }

        if (
            coloured.Count > AllowedColouredPatterns
            && coloured.Count > patterns.Count * AllowedColouredShare
        )
            yield return new Issue(
                GetTemplate("Sparingly"),
                beatmap,
                coloured.Count,
                patterns.Count
            ).ForDifficulties(Difficulty.Hard);
    }
}
