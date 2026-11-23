using System.Globalization;
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
public class CheckManiaStreams : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Mode.Mania],
            Difficulties = [Difficulty.Normal, Difficulty.Hard, Difficulty.Insane],
            Category = "Compose",
            Message = "Too demanding streams for the difficulty.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out streams that the ranking criteria consider too demanding for the difficulty:

| Difficulty | Pointed out |
|---|---|
| Normal | 1/2 streams longer than 8 measures without a pause |
| Hard | 1/4 streams longer than 8 measures without a pause, chords in 1/4 streams less than 1/1 apart |
| Insane | streams of long notes at 1/6 or faster that last at least 1 beat |"
                },
                {
                    "Reasoning",
                    @"
                    Long streams need pauses for players to rest, and dense chords or very short long notes inside of streams take more
                    stamina and precision than players of the level have.

                    The ranking criteria ask for a pause after every musical phrase, which is usually 4 measures long. Where a phrase
                    starts can't be told from the beatmap, so only streams longer than two phrases are pointed out. Those go
                    through at least one whole phrase without any pause, no matter where it starts. Any gap slower than the stream counts
                    as a pause, so check shorter streams and the length of the pauses yourself.

                    Ranked Hards often have chords in 1/4 streams closer than 1/1, so those are only minor issues. With 7 or more keys,
                    they are only pointed out when they go on for more than 2 beats, like the ranking criteria allow for chordstreams there.
                    Only streams with a constant interval and no minijacks count there, and each difficulty gets one issue listing where
                    every stretch of them starts. Chords at the start or end of those streams count too, since unlike for 1/6 streams the
                    ranking criteria don't make an exception for them.

                    Long notes held for longer than 1 beat don't count as part of a long note stream.

                    Snaps are counted in beats of the song. Following the Scaling BPM page, from 240 BPM a beat counts double
                    and below 120 BPM it counts half. Difficulties whose level can't be told from their name are skipped."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Pause",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} {1} stream for {2} measures without a {3} pause.",
                    "timestamp -",
                    "snap",
                    "measures",
                    "pause"
                )
                    .WithCause(
                        "A 1/2 stream in a Normal or 1/4 stream in a Hard is longer than 8 measures without any pause."
                    )
                    .WithRule(RC.Mania.Normal_LongStreams12Notes, RC.Mania.Hard_LongStreams14Notes)
            },
            {
                "Chords",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Chords less than 1/1 apart in 1/4 streams, {1} in total.",
                    "timestamps -",
                    "count"
                )
                    .WithCause("Chords in a 1/4 stream follow each other too closely in a Hard.")
                    .WithRule(RC.Mania.Hard_ChordsWithin14Streams)
            },
            {
                "LongNotes",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Stream of {1} long notes at 1/6 or faster.",
                    "timestamp -",
                    "count"
                )
                    .WithCause(
                        "Short long notes follow each other at 1/6 or faster for at least 1 beat in an Insane."
                    )
                    .WithRule(RC.Mania.Insane_AvoidUsing16Higher)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        if (beatmap.IsDifficultyGuessed())
            yield break;

        foreach (
            var (divisor, pause, difficulty) in new[]
            {
                (2d, "1/1", Difficulty.Normal),
                (4d, "1/2", Difficulty.Hard),
            }
        )
        foreach (var (run, measures) in ManiaPatterns.PhrasesWithoutPause(beatmap, divisor, 2))
            yield return new Issue(
                GetTemplate("Pause"),
                beatmap,
                Timestamp.Get(run.Objects),
                $"1/{divisor:0}",
                measures.ToString("0.#", CultureInfo.InvariantCulture),
                pause
            ).ForDifficulties(difficulty);

        var allowsChordstreams = ManiaPatterns.GetKeys(beatmap) >= 7;

        // From 7 keys, chordstreams may last for up to 2 beats.
        var closeChords = ManiaPatterns
            .CloseChords(beatmap, 4, 6, 1)
            .Where(run =>
                !allowsChordstreams
                || ManiaPatterns.Beats(beatmap, run.Objects[0].time, run.Objects[^1].time) >= 2.1
            )
            .ToList();

        // Often the whole difficulty is mapped like this, so it's one issue with where each stretch starts.
        if (closeChords.Count > 0)
            yield return new Issue(
                GetTemplate("Chords"),
                beatmap,
                string.Join(
                    " ",
                    closeChords.Select(run =>
                        Timestamp
                            .Get(
                                run.Objects.TakeWhile(o => o.time - run.Objects[0].time < 2)
                                    .ToArray()
                            )
                            .Trim()
                    )
                ),
                closeChords.Sum(run => run.Count)
            ).ForDifficulties(Difficulty.Hard);

        foreach (var run in ManiaPatterns.LongNoteStreams(beatmap, 6))
            yield return new Issue(
                GetTemplate("LongNotes"),
                beatmap,
                Timestamp.Get(run.Objects[0], run.Objects[^1]),
                run.Count
            ).ForDifficulties(Difficulty.Insane);
    }
}
