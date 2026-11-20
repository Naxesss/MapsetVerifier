using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Mania.Compose;

[Check]
public class CheckManiaRhythmPatterns : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Mode.Mania],
            Difficulties = [Difficulty.Easy, Difficulty.Normal, Difficulty.Hard, Difficulty.Insane],
            Category = "Compose",
            Message = "Too dense rhythm patterns for the difficulty.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out rhythm that the ranking criteria consider too demanding for lower difficulties:

| Difficulty | Pointed out |
|---|---|
| Easy | 1/4 or faster notes in a row, more than 5 notes in a row at 1/2, chords inside 1/2 patterns |
| Normal | 1/6 or faster notes in a row, more than 5 notes in a row at 1/4, chords inside 1/4 patterns |
| Hard | more than 4 notes in a row at 1/8 (3 grace notes are fine), chords inside 1/6 streams |
| Insane | chords inside 1/8 streams |"
                },
                {
                    "Reasoning",
                    @"
                    Each difficulty level has rhythms its players can keep up with. Chords at the start or end of a pattern are
                    fine, but chords in the middle of one break the flow of the hands.

                    Snaps are counted in beats of the song. Following the Scaling BPM page, from 240 BPM a beat counts double
                    (1/2 is as dense as 1/4) and below 120 BPM it counts half.

                    Ranked Easies often put chords in 1/2 patterns, ranked Hards use short 1/8 runs, and ranked Insanes use
                    chords in 1/8 streams, so those are only minor issues."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Snap",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} {1} notes in a row at {2} or faster.",
                    "timestamp -",
                    "count",
                    "snap"
                )
                    .WithCause(
                        "Notes follow each other at a snap that is too fast for the difficulty."
                    )
                    .WithRule(
                        RC.Mania.Easy_NoteSnappingsConsecutive14,
                        RC.Mania.Normal_NoteSnappingsConsecutive16
                    )
            },
            {
                "SnapMinor",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} {1} notes in a row at {2} or faster.",
                    "timestamp -",
                    "count",
                    "snap"
                )
                    .WithCause("More than 4 notes follow each other at 1/8 or faster in a Hard.")
                    .WithRule(RC.Mania.Hard_NoteSnappingsConsecutive18)
            },
            {
                "Run",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} {1} notes in a row at {2}, more than 5.",
                    "timestamp -",
                    "count",
                    "snap"
                )
                    .WithCause(
                        "More than 5 notes follow each other at 1/2 in an Easy, or at 1/4 in a Normal."
                    )
                    .WithRule(
                        RC.Mania.Easy_AvoidUsingMore5Consecutive,
                        RC.Mania.Normal_AvoidUsingMore5Consecutive
                    )
            },
            {
                "Chord",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Chord in the middle of a {1} pattern.",
                    "timestamp -",
                    "snap"
                )
                    .WithCause("A chord is used inside a pattern rather than at its start or end.")
                    .WithRule(
                        RC.Mania.Normal_AvoidUsingChordsDuringConsecutive,
                        RC.Mania.Hard_AvoidUsingChordsMiddle1
                    )
            },
            {
                "ChordMinor",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Chord in the middle of a {1} pattern.",
                    "timestamp -",
                    "snap"
                )
                    .WithCause(
                        "A chord is used inside a 1/2 pattern in an Easy, or a 1/8 stream in an Insane."
                    )
                    .WithRule(
                        RC.Mania.Easy_AvoidUsingChordsDuringConsecutive,
                        RC.Mania.Insane_AvoidUsingChordsMiddle1
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        // Consecutive snaps that are too fast: Easy 1/4, Normal 1/6, Hard 1/8 with up to 3 grace notes.
        foreach (var issue in FastRuns(beatmap, 4, 3, "Snap", Difficulty.Easy))
            yield return issue;

        foreach (var issue in FastRuns(beatmap, 6, 3, "Snap", Difficulty.Normal))
            yield return issue;

        foreach (var issue in FastRuns(beatmap, 8, 5, "SnapMinor", Difficulty.Hard))
            yield return issue;

        // More than 5 notes in a row: Easy 1/2, Normal 1/4.
        foreach (var issue in FastRuns(beatmap, 2, 6, "Run", Difficulty.Easy))
            yield return issue;

        foreach (var issue in FastRuns(beatmap, 4, 6, "Run", Difficulty.Normal))
            yield return issue;

        // Chords inside patterns.
        foreach (
            var (divisor, template, difficulty) in new[]
            {
                (2d, "ChordMinor", Difficulty.Easy),
                (4d, "Chord", Difficulty.Normal),
                (6d, "Chord", Difficulty.Hard),
                (8d, "ChordMinor", Difficulty.Insane),
            }
        )
        foreach (var row in ManiaPatterns.ChordsInsideRuns(beatmap, divisor))
            yield return new Issue(
                GetTemplate(template),
                beatmap,
                Timestamp.Get(row.Objects),
                Snap(divisor)
            ).ForDifficulties(difficulty);
    }

    private IEnumerable<Issue> FastRuns(
        Beatmap beatmap,
        double divisor,
        int minRows,
        string template,
        Difficulty difficulty
    ) =>
        ManiaPatterns
            .RowRuns(beatmap, divisor)
            .Where(run => run.Count >= minRows)
            .Select(run =>
                new Issue(
                    GetTemplate(template),
                    beatmap,
                    Timestamp.Get(run[0].Objects.Concat(run[^1].Objects).ToArray()),
                    run.Count,
                    Snap(divisor)
                ).ForDifficulties(difficulty)
            );

    private static string Snap(double divisor) => $"1/{divisor:0}";
}
