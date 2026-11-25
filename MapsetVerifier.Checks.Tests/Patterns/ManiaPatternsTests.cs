using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Patterns;

public class ManiaPatternsTests
{
    private const string Red = "0,500,4,2,0,100,1,0";

    /// <summary> Notes as "time:columns", with ">end" for long notes, in 4K at 120 bpm. </summary>
    private static Beatmap Map(string notes, params string[] timing)
    {
        var lines = notes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(entry =>
            {
                var parts = entry.Split(':', '>');
                return parts[1]
                    .Select(column =>
                    {
                        var x = (column - '1') * 128 + 64;
                        return parts.Length > 2
                            ? $"{x},192,{parts[0]},128,0,{parts[2]}:0:0:0:0:"
                            : $"{x},192,{parts[0]},1,0,0:0:0:0:";
                    });
            });

        return new Beatmap(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .TimingPoints(timing.Length > 0 ? timing : [Red])
                .HitObjects(lines)
                .Build(),
            "song",
            $"{Guid.NewGuid():N}.osu"
        );
    }

    private static int[] Counts(IEnumerable<ManiaPatterns.Run> runs) =>
        runs.Select(run => run.Count).ToArray();

    [Fact]
    public void Beats_WithoutRedLines_AreHalfASecond()
    {
        Assert.Equal(2, ManiaPatterns.Beats(Map("0:1", "0,-100,4,2,0,100,0,0"), 0, 1000));
    }

    [Theory]
    [InlineData(250, 2)] // 240 bpm, where a beat counts double.
    [InlineData(500, 2)]
    [InlineData(1000, 2)] // 60 bpm, where it counts half.
    public void Beats_AreAlikeAtAnySpeed(double beatLength, double expected)
    {
        Assert.Equal(
            expected,
            ManiaPatterns.Beats(Map("0:1", $"0,{beatLength},4,2,0,100,1,0"), 0, 1000)
        );
    }

    [Theory]
    [InlineData("0:1 125:1 250:1", new[] { 3 })]
    [InlineData("0:1 125:1 250:2 375:2 500:2", new[] { 2, 3 })]
    [InlineData("0:1 125:1 1000:1 1125:1", new[] { 2, 2 })]
    [InlineData("0:12 125:12", new[] { 2, 2 })]
    [InlineData("0:1 125:2 250:1", new int[0])]
    public void Jacks(string notes, int[] expected)
    {
        Assert.Equal(expected, Counts(ManiaPatterns.Jacks(Map(notes), 4)));
    }

    [Theory]
    [InlineData("0:1 125:2 250:1 375:3 500:1", new[] { 3 })]
    [InlineData("0:1 125:2 250:1 375:2 500:1", new int[0])] // A trill.
    [InlineData("0:1 125:23 250:1 375:2 500:1", new[] { 3 })]
    // Interrupted, and then again in the same stream.
    [InlineData(
        "0:1 125:2 250:1 375:3 500:1 625:2 750:3 875:1 1000:2 1125:1 1250:3 1375:1",
        new[] { 3, 3 }
    )]
    public void Anchors(string notes, int[] expected)
    {
        Assert.Equal(expected, Counts(ManiaPatterns.Anchors(Map(notes), 4)));
    }

    [Theory]
    [InlineData("0:1 125:2 250:1 375:2", new[] { 4 })]
    [InlineData("0:1 125:2 250:1 375:2 500:3 625:4 750:3 875:4 1000:3", new[] { 4, 5 })]
    [InlineData("0:1 125:2 250:1 375:2 500:12 625:1", new[] { 4 })]
    [InlineData("0:1 125:2 250:1", new int[0])]
    public void Trills(string notes, int[] expected)
    {
        Assert.Equal(expected, Counts(ManiaPatterns.Trills(Map(notes), 4)));
    }

    [Theory]
    [InlineData("0:1 125:2 250:3 375:4 500:1", new[] { 5 })]
    [InlineData("0:1 125:2 250:3", new int[0])] // Less than a beat.
    [InlineData("0:1 125:2 250:3 375:4 500:1 625:1 750:2 875:3 1000:4 1125:1", new[] { 5, 5 })]
    // 1/4 into 1/3, where the row in between is part of both.
    [InlineData("0:1 125:2 250:3 375:4 500:1 667:2 833:3 1000:4", new[] { 5, 4 })]
    // 1/8 is too fast.
    [InlineData("0:1 125:2 250:3 375:4 500:1 562:2", new[] { 5 })]
    public void ConstantStreams(string notes, int[] expected)
    {
        Assert.Equal(
            expected,
            ManiaPatterns.ConstantStreams(Map(notes), 3, 8).Select(stream => stream.Count)
        );
    }

    [Theory]
    [InlineData("0:12 125:3 250:14 375:2 500:13", new[] { 3 })]
    [InlineData("0:12 125:3 250:1 375:2 500:34 625:1 750:24", new[] { 2 })]
    [InlineData("0:12 125:3 250:14 375:2 500:1 625:3 750:24 875:1 1000:23", new[] { 2, 2 })]
    [InlineData("0:1 125:2 250:3 375:4 500:12", new int[0])]
    public void CloseChords(string notes, int[] expected)
    {
        Assert.Equal(expected, Counts(ManiaPatterns.CloseChords(Map(notes), 4, 8, 1)));
    }

    [Theory]
    [InlineData("0:1>250 125:2>375 250:3>500 375:4>625 500:1>750", new[] { 5 })]
    [InlineData("0:1>250 125:2>375 250:3 375:4>625 500:1>750", new int[0])]
    [InlineData("0:1>900 125:2>900 250:3>900 375:4>900 500:1>900", new int[0])]
    public void LongNoteStreams(string notes, int[] expected)
    {
        Assert.Equal(expected, Counts(ManiaPatterns.LongNoteStreams(Map(notes), 4)));
    }

    [Fact]
    public void RowRuns_EndWhereTheSnapDoes()
    {
        Assert.Equal(
            [2, 3],
            ManiaPatterns
                .RowRuns(Map("0:1 125:2 1000:1 1125:2 1250:3 5000:1"), 4)
                .Select(run => run.Count)
        );
        Assert.Equal(
            [125d],
            ManiaPatterns
                .ChordsInsideRuns(Map("0:12 125:34 250:12 5000:12"), 4)
                .Select(row => row.Time)
        );
    }

    [Fact]
    public void Kiai_EndsWithItsLineOrTheLastObject()
    {
        Assert.Equal(
            [(1000, 3000, 1), (5000, 6000, 0.5)],
            ManiaPatterns.KiaiSections(
                Map(
                    "0:1 6000:1",
                    Red,
                    "1000,-100,4,2,0,100,0,1",
                    "3000,-100,4,2,0,100,0,0",
                    "5000,-100,4,2,0,100,0,1"
                )
            )
        );
    }

    [Fact]
    public void Kiai_PastTheLastObjectOrWithoutRedLines_IsNone()
    {
        Assert.Empty(ManiaPatterns.KiaiSections(Map("0:1", Red, "5000,-100,4,2,0,100,0,1")));
        Assert.Empty(ManiaPatterns.KiaiSections(Map("0:1 9000:1", "0,-100,4,2,0,100,0,1")));
        Assert.Empty(ManiaPatterns.KiaiSections(Map("", Red, "0,-100,4,2,0,100,0,1")));
    }

    [Theory]
    [InlineData("0:1>500 0:2>560", 1)]
    [InlineData("0:1>500 0:2>501", 0)]
    [InlineData("0:1>500 0:2>1000", 0)]
    public void CloseReleases(string notes, int expected)
    {
        Assert.Equal(expected, ManiaPatterns.CloseReleases(Map(notes), 0.5).Count());
    }

    [Theory]
    [InlineData("0:1>500 100:2>1000", 1)]
    [InlineData("0:1>500 100:2>501", 0)]
    [InlineData("0:1>500 100:3>1000", 0)]
    [InlineData("0:1>500 600:2>1000", 0)]
    public void AdjacentReleaseMismatches(string notes, int expected)
    {
        Assert.Equal(expected, ManiaPatterns.AdjacentReleaseMismatches(Map(notes)).Count());
    }

    [Fact]
    public void ScrollSections_ChangeWithTheSpeedOnly()
    {
        var sections = ManiaPatterns
            .ScrollSections(
                Map(
                    "0:1 1000:1 2000:1 3000:1 10000:1",
                    Red,
                    "1000,-50,4,2,0,100,0,0",
                    "1500,-50,4,2,0,80,0,0",
                    // Twice the bpm at half the speed.
                    "2000,250,4,2,0,100,1,0",
                    "2000,-200,4,2,0,100,0,0",
                    "3000,500,4,2,0,100,1,0"
                )
            )
            .Where(section => section.Beats > 0)
            .ToList();

        Assert.Equal([1, 2, 1], sections.Select(section => section.Multiplier));
        Assert.Equal([1, 1, 2], sections.Select(section => section.Objects.Count).Take(3));
    }

    [Fact]
    public void ScrollSections_WithoutLinesOrObjects_AreNone()
    {
        Assert.Empty(ManiaPatterns.ScrollSections(Map("", Red)));
        Assert.Empty(ManiaPatterns.ScrollSections(Map("", "0,-100,4,2,0,100,0,0")));
    }
}
