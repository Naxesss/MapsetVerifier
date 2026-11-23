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
public class CheckManiaLongNotes : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Mode.Mania],
            Difficulties = [Difficulty.Easy, Difficulty.Normal, Difficulty.Hard],
            Category = "Compose",
            Message = "Too short or tightly released long notes.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out long notes that are too hard to release for lower difficulties. In 7 key Normals, long notes
                    held together in neighbouring columns should also be released together.

| Difficulty | Held at least | Releases at least apart |
|---|---|---|
| Easy | 1/1 | 1/1 |
| Normal | 1/2 | 1/2 |
| Hard | 1/4 | |"
                },
                {
                    "Reasoning",
                    @"
                    Releasing on time is harder than pressing, and releasing different fingers shortly after each other needs a
                    level of finger independence players of lower difficulties don't have yet.

                    Snaps are counted in beats of the song. Following the Scaling BPM page, from 240 BPM a beat counts double
                    and below 120 BPM it counts half."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Short",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Long note held for less than {1}.",
                    "timestamp -",
                    "snap"
                )
                    .WithCause("A long note is shorter than the difficulty allows.")
                    .WithRule(
                        RC.Mania.Easy_LongNotesHeldLeastOne,
                        RC.Mania.Normal_LongNotesHeldLeast1,
                        RC.Mania.Hard_LongNotesHeldLeast1
                    )
            },
            {
                "Release",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Long notes released less than {1} apart.",
                    "timestamp -",
                    "snap"
                )
                    .WithCause(
                        "Two long notes end at different times that are closer than the difficulty allows."
                    )
                    .WithRule(
                        RC.Mania.Easy_LongNoteReleasesLeastOne,
                        RC.Mania.Normal_LongNoteReleasesLeast1
                    )
            },
            {
                "Adjacent",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Long notes held together in neighbouring columns are released at different times.",
                    "timestamp -"
                )
                    .WithCause(
                        "Two long notes in neighbouring columns of a 7 key Normal overlap but end apart."
                    )
                    .WithRule(RC.Mania.Normal_LongNotesPlayedAdjacentKeys)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        foreach (
            var (beats, snap, difficulty) in new[]
            {
                (1d, "1/1", Difficulty.Easy),
                (0.5d, "1/2", Difficulty.Normal),
                (0.25d, "1/4", Difficulty.Hard),
            }
        )
        foreach (var note in ManiaPatterns.ShortLongNotes(beatmap, beats))
            yield return new Issue(
                GetTemplate("Short"),
                beatmap,
                Timestamp.Get(note.Object),
                snap
            ).ForDifficulties(difficulty);

        foreach (
            var (beats, snap, difficulty) in new[]
            {
                (1d, "1/1", Difficulty.Easy),
                (0.5d, "1/2", Difficulty.Normal),
            }
        )
        foreach (var (first, second) in ManiaPatterns.CloseReleases(beatmap, beats))
            yield return new Issue(
                GetTemplate("Release"),
                beatmap,
                Timestamp.Get(first.Object, second.Object),
                snap
            ).ForDifficulties(difficulty);

        if (ManiaPatterns.GetKeys(beatmap) != 7)
            yield break;

        foreach (var (first, second) in ManiaPatterns.AdjacentReleaseMismatches(beatmap))
            yield return new Issue(
                GetTemplate("Adjacent"),
                beatmap,
                Timestamp.Get(first.Object, second.Object)
            ).ForDifficulties(Difficulty.Normal);
    }
}
