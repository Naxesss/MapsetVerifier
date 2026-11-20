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
public class CheckManiaColumnPatterns : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Mode.Mania],
            Difficulties = [Difficulty.Normal, Difficulty.Hard],
            Category = "Compose",
            Message = "Strenuous anchors, jacks or trills for the difficulty.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out column patterns that the ranking criteria consider too strenuous for lower difficulties.
                    An anchor is a column hit on every other note of a stream, a jack is the same column hit on notes right
                    after each other, and a trill alternates between the same two columns.

| Difficulty | Pointed out |
|---|---|
| Normal | anchors of more than 5 notes in 1/2 streams, 1/4 jacks |
| Hard | anchors of 5 or more notes in 1/4 streams, 1/4 jacks of 3 or more notes, trills of more than 9 notes |"
                },
                {
                    "Reasoning",
                    @"
                    These patterns put the strain on one or two fingers. Players of lower difficulties don't have the stamina
                    or finger independence for long runs of them yet.

                    Snaps are counted in beats of the song. Following the Scaling BPM page, from 240 BPM a beat counts double
                    and below 120 BPM it counts half.

                    The ranking criteria ask Normals to avoid anchors of 3 or more notes, but ranked Normals use short anchors
                    so often that only anchors of more than 5 notes are pointed out. Hards may use 1/4 minijacks with rest around
                    them, so only jacks of 3 or more notes are pointed out there. Both are minor issues."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Anchor",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Anchor of {1} notes, {2} or more.",
                    "timestamp -",
                    "count",
                    "limit"
                )
                    .WithCause(
                        "A column is hit on every other note of a stream too many times in a row."
                    )
                    .WithRule(
                        RC.Mania.Normal_AvoidAnchorsConsistingThreeMore,
                        RC.Mania.Hard_AvoidAnchorsConsistingFiveMore
                    )
            },
            {
                "Jack",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} 1/4 jack of {1} notes.",
                    "timestamp -",
                    "count"
                )
                    .WithCause("The same column is hit 1/4 apart in a Normal.")
                    .WithRule(RC.Mania.Normal_AvoidUsing14Minijacks)
            },
            {
                "JackMinor",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} 1/4 jack of {1} notes.",
                    "timestamp -",
                    "count"
                )
                    .WithCause("The same column is hit 3 or more times 1/4 apart in a Hard.")
                    .WithRule(RC.Mania.Hard_14JackUsageDiscouraged)
            },
            {
                "Trill",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Trill of {1} notes, more than 9.",
                    "timestamp -",
                    "count"
                )
                    .WithCause(
                        "More than 9 notes alternate between the same two columns in a Hard."
                    )
                    .WithRule(RC.Mania.Hard_AvoidUsingMore9Consecutive)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        // The ranking criteria say 3, but ranked Normals use short anchors so often that only longer ones are
        // worth a look; see the documentation.
        foreach (var run in ManiaPatterns.Anchors(beatmap, 2).Where(run => run.Count > 5))
            yield return Report("Anchor", beatmap, run, 6).ForDifficulties(Difficulty.Normal);

        foreach (var run in ManiaPatterns.Anchors(beatmap, 4).Where(run => run.Count >= 5))
            yield return Report("Anchor", beatmap, run, 5).ForDifficulties(Difficulty.Hard);

        foreach (var run in ManiaPatterns.Jacks(beatmap, 4))
        {
            yield return Report("Jack", beatmap, run).ForDifficulties(Difficulty.Normal);

            // Minijacks with rest around them are fine in Hards, so only longer jacks are pointed out.
            if (run.Count >= 3)
                yield return Report("JackMinor", beatmap, run).ForDifficulties(Difficulty.Hard);
        }

        foreach (var run in ManiaPatterns.Trills(beatmap, 4).Where(run => run.Count > 9))
            yield return Report("Trill", beatmap, run).ForDifficulties(Difficulty.Hard);
    }

    private Issue Report(
        string template,
        Beatmap beatmap,
        ManiaPatterns.Run run,
        params object[] extra
    ) =>
        new(
            GetTemplate(template),
            beatmap,
            [Timestamp.Get(run.Objects[0], run.Objects[^1]), run.Count, .. extra]
        );
}
