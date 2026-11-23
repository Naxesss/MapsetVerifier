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
public class CheckManiaShieldsAndBrackets : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Mode.Mania],
            Difficulties = [Difficulty.Normal],
            Category = "Compose",
            Message = "1/4 shields or brackets.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out patterns in Normals that the ranking criteria consider too hard to coordinate at this level:

| Pattern | What it is |
|---|---|
| 1/4 shield | a note in the same column, 1/4 or less before a long note |
| 1/4 bracket | trills between neighbouring columns played at the same time, such as columns 1 and 3 alternating with column 2 |"
                },
                {
                    "Reasoning",
                    @"
                    Players of Normals can't yet move their fingers independently enough for these patterns.

                    Brackets are only pointed out with 5 or more keys. The ranking criteria list them for 7 key Normals, and 4 key
                    guidelines don't mention them.

                    Snaps are counted in beats of the song. Following the Scaling BPM page, from 240 BPM a beat counts double
                    and below 120 BPM it counts half. Difficulties whose level can't be told from their name are skipped."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Shield",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} 1/4 shield before a long note.",
                    "timestamp -"
                )
                    .WithCause(
                        "A note is placed 1/4 or less before a long note in the same column of a Normal."
                    )
                    .WithRule(RC.Mania.Normal_AvoidUsingForm14)
            },
            {
                "Bracket",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} 1/4 bracket of {1} rows.",
                    "timestamp -",
                    "count"
                )
                    .WithCause(
                        "Several trills between neighbouring columns are played at once at 1/4 in a Normal with 5 or more keys."
                    )
                    .WithRule(RC.Mania.Normal_Avoid14BracketPatterns)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        if (beatmap.IsDifficultyGuessed())
            yield break;

        foreach (var (note, longNote) in ManiaPatterns.Shields(beatmap, 4))
            yield return new Issue(
                GetTemplate("Shield"),
                beatmap,
                Timestamp.Get(note.Object, longNote.Object)
            ).ForDifficulties(Difficulty.Normal);

        if (ManiaPatterns.GetKeys(beatmap) < 5)
            yield break;

        foreach (var run in ManiaPatterns.Brackets(beatmap, 4))
            yield return new Issue(
                GetTemplate("Bracket"),
                beatmap,
                Timestamp.Get(run.Objects),
                run.Count
            ).ForDifficulties(Difficulty.Normal);
    }
}
