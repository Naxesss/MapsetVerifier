using MapsetVerifier.Checks.Mania.Compose;
using MapsetVerifier.Checks.Mania.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Compose;

/// <summary>
///     All maps here are 7 key at 150 BPM unless given, so 1/1 is 400 ms, 1/4 is 100 ms and 1/6 is 67 ms.
/// </summary>
public class ManiaStreamChecksTests
{
    private const int Beat = 400;

    [Fact]
    public void QuarterShield_IsReportedInNormal()
    {
        using var context = Create([Note(1000, 1), Hold(1100, 1500, 1)]);

        var issue = Assert.Single(
            Run<CheckManiaShieldsAndBrackets>(context, Beatmap.Difficulty.Normal)
        );
        Assert.Contains("1/4 shield", issue.message);
        Assert.Equal(Issue.Level.Warning, issue.level);
    }

    [Fact]
    public void QuarterShield_IsFineInHard()
    {
        using var context = Create([Note(1000, 1), Hold(1100, 1500, 1)]);

        Assert.Empty(Run<CheckManiaShieldsAndBrackets>(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void OtherNotesBeforeLongNotes_AreNoShields()
    {
        // In another column, 1/2 apart, and between two long notes.
        using var context = Create([
            Note(1000, 1),
            Hold(1100, 1500, 2),
            Note(3000, 3),
            Hold(3200, 3600, 3),
            Hold(5000, 5050, 4),
            Hold(5100, 5500, 4),
        ]);

        Assert.Empty(Run<CheckManiaShieldsAndBrackets>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void QuarterBracket_IsReportedInNormalOnce()
    {
        using var context = Create(Bracket());

        var issue = Assert.Single(
            Run<CheckManiaShieldsAndBrackets>(context, Beatmap.Difficulty.Normal)
        );
        Assert.Contains("1/4 bracket of 4 rows", issue.message);
    }

    [Fact]
    public void QuarterBracket_IsFineIn4K()
    {
        using var context = Create(Bracket(keys: 4), keys: 4);

        Assert.Single(Utils.ManiaPatterns.Brackets(context.GetBeatmap("Test"), 4));
        Assert.Empty(Run<CheckManiaShieldsAndBrackets>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void HalfBracket_IsFineInNormal()
    {
        using var context = Create(Bracket(Beat / 2));

        Assert.Empty(Run<CheckManiaShieldsAndBrackets>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void SingleTrillAndSpreadTrills_AreNoBrackets()
    {
        // A plain trill, and a chord alternating with a column that isn't next to it.
        using var context = Create([
            Note(1000, 1),
            Note(1100, 2),
            Note(1200, 1),
            Note(3000, 1),
            Note(3000, 2),
            Note(3100, 6),
            Note(3200, 1),
            Note(3200, 2),
        ]);

        Assert.Empty(Run<CheckManiaShieldsAndBrackets>(context, Beatmap.Difficulty.Normal));
    }

    [Theory]
    [InlineData(Beatmap.Difficulty.Normal, 2, "1/1")]
    [InlineData(Beatmap.Difficulty.Hard, 4, "1/2")]
    public void StreamOfNineMeasures_IsReported(
        Beatmap.Difficulty difficulty,
        int divisor,
        string pause
    )
    {
        using var context = Create(Stream(1000, Beat / divisor, 9 * 4 * divisor + 1));

        var issue = Assert.Single(Run<CheckManiaStreams>(context, difficulty));
        Assert.Contains(
            $"1/{divisor} stream for 9 measures without a {pause} pause",
            issue.message
        );
    }

    [Fact]
    public void StreamOfEightMeasures_IsFine()
    {
        using var context = Create(Stream(1000, Beat / 2, 8 * 4 * 2 + 1));

        Assert.Empty(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void LongStreamWithPause_IsFine()
    {
        var first = Stream(1000, Beat / 2, 6 * 4 * 2 + 1);
        var second = Stream(1000 + 6 * 4 * Beat + Beat, Beat / 2, 6 * 4 * 2 + 1);
        using var context = Create(first.Concat(second));

        Assert.Empty(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void SlowSong_KeepsWholeMeasures()
    {
        // At 100 BPM 1/4 (150 ms) is as dense as 1/2, but 72 of those gaps are only 4.5 measures of the song.
        using var context = Create(Stream(1000, 150, 73), bpm: 100);

        Assert.Empty(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void ChordsHalfApartInStream_AreReportedInHard()
    {
        using var context = Create(ChordStream(9, 2, keys: 4), keys: 4);

        var issue = Assert.Single(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Hard));
        Assert.Contains("Chords less than 1/1 apart in 1/4 streams, 5 in total.", issue.message);
        Assert.Equal(Issue.Level.Minor, issue.level);
    }

    [Theory]
    [InlineData(17, 4)] // 1/1 apart.
    [InlineData(3, 2)] // Three rows at 1/4 don't last for a beat, so they aren't a stream.
    public void OtherChords_AreFineInHard(int rows, int chordEvery)
    {
        using var context = Create(ChordStream(rows, chordEvery, keys: 4), keys: 4);

        Assert.Empty(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void ChordsAtStartAndEndOfStream_AreReportedInHard()
    {
        // Unlike for 1/6 streams, there is no exception for these. The chords are 3/4 apart in a stream of 1 beat.
        using var context = Create(
            Stream(1000, 100, 5, 4).Append(Note(1000, 3, 4)).Append(Note(1300, 2, 4)),
            keys: 4
        );

        Assert.Single(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void SeveralStretches_AreOneIssueWithATimestampEach()
    {
        // The chords of the two streams are far apart.
        using var context = Create(
            ChordStream(9, 2, keys: 4).Concat(ChordStream(9, 2, keys: 4, start: 5000)),
            keys: 4
        );

        var issue = Assert.Single(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Hard));
        Assert.StartsWith(
            "00:01:000 (1000|0,1000|2) - 00:05:000 (5000|0,5000|2) - Chords",
            issue.message
        );
        Assert.Contains("10 in total", issue.message);
    }

    [Fact]
    public void ChordsInUnevenPattern_AreFineInHard()
    {
        // Alternating 1/4 and 1/8 gaps aren't a constant interval.
        var times = Enumerable.Range(0, 8).SelectMany(i => new[] { i * 150, i * 150 + 50 });
        using var context = Create(
            times
                .Select((time, i) => Note(1000 + time, i % 4 + 1, 4))
                .Concat(Enumerable.Range(0, 4).Select(i => Note(1000 + i * 300, 3, 4))),
            keys: 4
        );

        Assert.Empty(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void ChordsBetweenMinijacks_AreFineInHard()
    {
        // Every second row repeats a column of the row before it.
        using var context = Create(
            Enumerable
                .Range(0, 8)
                .SelectMany(i =>
                    new[]
                    {
                        Note(1000 + i * 200, 1, 4),
                        Note(1000 + i * 200, 2, 4),
                        Note(1100 + i * 200, 2, 4),
                    }
                ),
            keys: 4
        );

        Assert.Empty(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Hard));
    }

    [Theory]
    [InlineData(67)] // 1/6 streams have their own guideline.
    [InlineData(200)] // 1/2
    public void ChordsInOtherSnaps_AreFineInHard(int gap)
    {
        using var context = Create(ChordStream(13, 2, keys: 4, gap: gap), keys: 4);

        Assert.Empty(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void StreamAfterFasterBurst_IsStillReportedInHard()
    {
        // Two 1/8 gaps lead into a 1/4 stream with chords 1/2 apart.
        using var context = Create(
            ChordStream(9, 2, keys: 4).Append(Note(900, 3, 4)).Append(Note(950, 2, 4)),
            keys: 4
        );

        var issue = Assert.Single(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Hard));
        Assert.Contains("5 in total", issue.message);
    }

    [Fact]
    public void ShortChordstream_IsFineIn7KUpTo2Beats()
    {
        using var twoBeats = Create(ChordStream(9, 2));
        using var threeBeats = Create(ChordStream(13, 2));

        Assert.Empty(Run<CheckManiaStreams>(twoBeats, Beatmap.Difficulty.Hard));
        Assert.Single(Run<CheckManiaStreams>(threeBeats, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void LongNoteStream_IsReportedInInsane()
    {
        using var context = Create(LongNotes(7, 100));

        var issue = Assert.Single(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Insane));
        Assert.Contains("Stream of 7 long notes at 1/6", issue.message);
    }

    [Theory]
    [InlineData(4, 100, 67)] // Shorter than 1 beat.
    [InlineData(7, 1000, 67)] // Staggered long notes that are held on.
    [InlineData(9, 80, 100)] // 1/4 apart.
    public void OtherLongNotes_AreFineInInsane(int count, int length, int gap)
    {
        using var context = Create(LongNotes(count, length, gap));

        Assert.Empty(Run<CheckManiaStreams>(context, Beatmap.Difficulty.Insane));
    }

    [Fact]
    public void GuessedDifficulty_IsSkipped()
    {
        using var shield = Create([Note(1000, 1), Hold(1100, 1500, 1)]);
        using var stream = Create(Stream(1000, Beat / 2, 200));
        var beatmap = shield.GetBeatmap("Test");

        Assert.True(beatmap.IsDifficultyGuessed());
        Assert.Empty(new CheckManiaShieldsAndBrackets().GetIssues(beatmap));
        Assert.Empty(new CheckManiaStreams().GetIssues(stream.GetBeatmap("Test")));
    }

    [Theory]
    [InlineData(4, 4, 1)] // Four times one measure.
    [InlineData(3, 4, 0)] // A few short ones are fine.
    [InlineData(6, 8, 0)] // Two measures aren't short.
    public void ShortKiai_IsReportedFromFour(int count, int beats, int issues)
    {
        var timing = new List<string> { TestTimingPoints.Uninherited(0, Beat) };

        for (var i = 0; i < count; i++)
        {
            timing.Add(TestTimingPoints.Inherited(i * 8000 + 4000, -100, effects: 1));
            timing.Add(TestTimingPoints.Inherited(i * 8000 + 4000 + beats * Beat, -100));
        }

        using var context = Create([Note(1000, 1), Note(100000, 1)], timing);

        Assert.Equal(
            issues,
            new CheckManiaKiaiSpam().GetIssues(context.GetBeatmap("Test")).Count()
        );
    }

    [Fact]
    public void RedLinesInsideKiai_DoNotSplitIt()
    {
        // Every measure has a red line without kiai, with a green line turning it back on at the same time.
        var timing = new List<string>();

        for (var i = 0; i < 8; i++)
        {
            timing.Add(TestTimingPoints.Uninherited(i * 4 * Beat, Beat));
            timing.Add(TestTimingPoints.Inherited(i * 4 * Beat, -100, effects: 1));
        }

        using var context = Create([Note(0, 1), Note(8 * 4 * Beat, 1)], timing);

        var section = Assert.Single(Utils.ManiaPatterns.KiaiSections(context.GetBeatmap("Test")));
        Assert.Equal(8, section.Measures, 3);
        Assert.Empty(new CheckManiaKiaiSpam().GetIssues(context.GetBeatmap("Test")));
    }

    private static List<Issue> Run<TCheck>(CheckTestContext context, Beatmap.Difficulty difficulty)
        where TCheck : BeatmapCheck, new()
    {
        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = difficulty;

        return new TCheck()
            .GetIssues(beatmap)
            .Where(issue =>
                issue.InterpretationPairs.Any(pair =>
                    pair.Key == "difficulty" && pair.Value == (int)difficulty
                )
            )
            .ToList();
    }

    /// <summary> Columns 1 and 3 alternating with column 2. </summary>
    private static IEnumerable<string> Bracket(int gap = Beat / 4, int keys = 7) =>
        [
            Note(1000, 1, keys),
            Note(1000, 3, keys),
            Note(1000 + gap, 2, keys),
            Note(1000 + gap * 2, 1, keys),
            Note(1000 + gap * 2, 3, keys),
            Note(1000 + gap * 3, 2, keys),
        ];

    /// <summary> Single notes cycling through four columns. </summary>
    private static IEnumerable<string> Stream(int start, int gap, int count, int keys = 7) =>
        Enumerable.Range(0, count).Select(i => Note(start + i * gap, i % 4 + 1, keys));

    /// <summary> A stream, 1/4 unless given, with a chord on every so many rows. </summary>
    private static IEnumerable<string> ChordStream(
        int rows,
        int chordEvery,
        int keys = 7,
        int start = 1000,
        int gap = Beat / 4
    ) =>
        Stream(start, gap, rows, keys)
            .Concat(
                Enumerable
                    .Range(0, rows)
                    .Where(i => i % chordEvery == 0)
                    .Select(i => Note(start + i * gap, (i + 2) % 4 + 1, keys))
            );

    /// <summary> Long notes cycling through all columns. </summary>
    private static IEnumerable<string> LongNotes(int count, int length, int gap = 67) =>
        Enumerable
            .Range(0, count)
            .Select(i => Hold(1000 + i * gap, 1000 + i * gap + length, i % 7 + 1));

    private static int X(int column, int keys) => (int)((column - 0.5) * 512 / keys);

    private static string Note(int time, int column, int keys = 7) =>
        $"{X(column, keys)},192,{time},1,0,0:0:0:0:";

    private static string Hold(int time, int endTime, int column) =>
        $"{X(column, 7)},192,{time},128,0,{endTime}:0:0:0:0:";

    private static CheckTestContext Create(
        IEnumerable<string> hitObjects,
        IEnumerable<string>? timing = null,
        double bpm = 150,
        int keys = 7
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .Version("Test")
                .CircleSize(keys)
                .TimingPoints(timing ?? [TestTimingPoints.Uninherited(0, 60000 / bpm)])
                .HitObjects(hitObjects.OrderBy(line => int.Parse(line.Split(',')[2])))
        );
}
