using MapsetVerifier.Checks.Mania.Compose;
using MapsetVerifier.Checks.Mania.Timing;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;
using static MapsetVerifier.Checks.Tests.Mania.ManiaOsu;

namespace MapsetVerifier.Checks.Tests.Mania.Compose;

/// <summary> All maps here are 4 key at 150 BPM, so 1/1 is 400 ms, 1/2 is 200 ms and 1/4 is 100 ms. </summary>
public class ManiaPatternChecksTests
{
    private const int Beat = 400;

    private static readonly int[] Columns = [Column1, Column2, Column3, Column4];

    [Fact]
    public void FiveHalfNotesInARow_AreAllowedInEasy()
    {
        using var context = Create(Stream(1000, Beat / 2, 5));

        Assert.Empty(Run<CheckManiaRhythmPatterns>(context, Beatmap.Difficulty.Easy));
    }

    [Fact]
    public void SixHalfNotesInARow_AreReportedInEasy()
    {
        using var context = Create(Stream(1000, Beat / 2, 6));

        var issue = Assert.Single(Run<CheckManiaRhythmPatterns>(context, Beatmap.Difficulty.Easy));
        Assert.Contains("6 notes in a row at 1/2, more than 5", issue.message);
    }

    [Fact]
    public void QuarterNotesAt100Bpm_CountAs1_2()
    {
        // Below 120 BPM a beat counts half, so 1/4 at 100 BPM (150 ms) is only as dense as 1/2.
        using var context = Create(Stream(1000, 150, 3), bpm: 100);

        Assert.Empty(Run<CheckManiaRhythmPatterns>(context, Beatmap.Difficulty.Easy));
    }

    [Fact]
    public void HalfNotesAt250Bpm_CountAs1_4()
    {
        // From 240 BPM a beat counts double, so 1/2 at 250 BPM (120 ms) is as dense as 1/4.
        using var context = Create(Stream(1000, 120, 3), bpm: 250);

        var issue = Assert.Single(Run<CheckManiaRhythmPatterns>(context, Beatmap.Difficulty.Easy));
        Assert.Contains("3 notes in a row at 1/4 or faster", issue.message);
    }

    [Fact]
    public void ChordInsideHalfPattern_IsReportedInEasy()
    {
        var notes = Stream(1000, Beat / 2, 4).Append(Note(1000 + Beat / 2, column: Column4));
        using var context = Create(notes);

        var issue = Assert.Single(Run<CheckManiaRhythmPatterns>(context, Beatmap.Difficulty.Easy));
        Assert.Contains("Chord in the middle of a 1/2 pattern", issue.message);
    }

    [Fact]
    public void ChordAtStartOfPattern_IsFine()
    {
        var notes = Stream(1000, Beat / 2, 4).Append(Note(1000, column: Column4));
        using var context = Create(notes);

        Assert.Empty(Run<CheckManiaRhythmPatterns>(context, Beatmap.Difficulty.Easy));
    }

    [Fact]
    public void ShortAnchor_IsOnlyMeasuredInNormal()
    {
        // Column 1 on every other note of a 1/2 stream: 1 2 1 3 1.
        int[] columns = [Column1, Column2, Column1, Column3, Column1];
        using var context = Create(
            columns.Select((column, i) => Note(1000 + i * Beat / 2, column: column))
        );

        var anchor = Assert.Single(ManiaPatterns.Anchors(context.GetBeatmap("Test"), 2));
        Assert.Equal(3, anchor.Count);
        Assert.Empty(Run<CheckManiaColumnPatterns>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void LongAnchor_IsReportedInNormal()
    {
        // Column 1 six times on every other note: 1 2 1 3 1 4 1 2 1 3 1.
        int[] columns =
        [
            Column1,
            Column2,
            Column1,
            Column3,
            Column1,
            Column4,
            Column1,
            Column2,
            Column1,
            Column3,
            Column1,
        ];
        using var context = Create(
            columns.Select((column, i) => Note(1000 + i * Beat / 2, column: column))
        );

        var issue = Assert.Single(
            Run<CheckManiaColumnPatterns>(context, Beatmap.Difficulty.Normal)
        );
        Assert.Contains("Anchor of 6 notes", issue.message);
    }

    [Fact]
    public void Trill_IsNotAnAnchor()
    {
        var notes = Enumerable
            .Range(0, 8)
            .Select(i => Note(1000 + i * Beat / 2, column: i % 2 == 0 ? Column1 : Column2));
        using var context = Create(notes);

        Assert.Empty(ManiaPatterns.Anchors(context.GetBeatmap("Test"), 2));
    }

    [Fact]
    public void QuarterJack_IsReportedInNormal()
    {
        using var context = Create([Note(1000), Note(1000 + Beat / 4)]);

        var issue = Assert.Single(
            Run<CheckManiaColumnPatterns>(context, Beatmap.Difficulty.Normal)
        );
        Assert.Contains("1/4 jack of 2 notes", issue.message);
        Assert.Equal(Issue.Level.Warning, issue.level);
    }

    [Fact]
    public void TrillOfTenNotes_IsReportedInHard()
    {
        var notes = Enumerable
            .Range(0, 10)
            .Select(i => Note(1000 + i * Beat / 4, column: i % 2 == 0 ? Column1 : Column2));
        using var context = Create(notes);

        Assert.Contains(
            Run<CheckManiaColumnPatterns>(context, Beatmap.Difficulty.Hard),
            issue => issue.message.Contains("Trill of 10 notes")
        );
    }

    [Fact]
    public void ShortLongNote_IsReportedInEasy()
    {
        using var context = Create([HoldNote(1000, 1000 + Beat / 2)]);

        var issue = Assert.Single(Run<CheckManiaLongNotes>(context, Beatmap.Difficulty.Easy));
        Assert.Contains("Long note held for less than 1/1", issue.message);
    }

    [Fact]
    public void CloseReleases_AreReportedInNormal()
    {
        using var context = Create([
            HoldNote(1000, 1000 + Beat * 2),
            HoldNote(1000, 1000 + Beat * 2 + Beat / 4, Column3),
        ]);

        var issue = Assert.Single(Run<CheckManiaLongNotes>(context, Beatmap.Difficulty.Normal));
        Assert.Contains("released less than 1/2 apart", issue.message);
    }

    [Fact]
    public void LongTermSlowdown_IsReportedInNormal()
    {
        // A 0.5x green line held for 16 beats with notes in it.
        var timing = new[]
        {
            TestTimingPoints.Uninherited(0, Beat, sampleSet: 1),
            TestTimingPoints.Inherited(2000, -200, sampleSet: 1),
            TestTimingPoints.Inherited(2000 + 16 * Beat, -100, sampleSet: 1),
        };
        var notes = Enumerable.Range(0, 40).Select(i => Note(1000 + i * Beat));
        using var context = Create(notes, timing);

        var issue = Assert.Single(Run<CheckManiaScrollSpeed>(context, Beatmap.Difficulty.Normal));
        Assert.Contains("Scrolls at 0.5x for 16 beats", issue.message);
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

    /// <summary> Single notes cycling through the columns, so no jacks or anchors form. </summary>
    private static IEnumerable<string> Stream(int start, int gap, int count) =>
        Enumerable.Range(0, count).Select(i => Note(start + i * gap, column: Columns[i % 4]));

    private static string HoldNote(int time, int endTime, int column = Column1) =>
        $"{column},192,{time},128,0,{endTime}:0:0:0:0:";

    private static CheckTestContext Create(
        IEnumerable<string> hitObjects,
        IEnumerable<string>? timingPoints = null,
        double bpm = 150
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .Version("Test")
                .TimingPoints(
                    timingPoints ?? [TestTimingPoints.Uninherited(0, 60000 / bpm, sampleSet: 1)]
                )
                .HitObjects(hitObjects.OrderBy(line => int.Parse(line.Split(',')[2])))
        );
}
