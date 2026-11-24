using MapsetVerifier.Checks.Taiko.Compose;
using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko;

/// <summary> Maps are 150 BPM unless stated otherwise, so 1/1 is 400 ms and 1/4 is 100 ms. </summary>
public class TaikoPatternChecksTests
{
    private const int Clap = 8;
    private const int Beat = 400;
    private const int Quarter = Beat / 4;

    [Theory]
    [InlineData("dk")]
    [InlineData("kdk")] // Not longer than three notes.
    [InlineData("ddddd")]
    [InlineData("kdddd")]
    [InlineData("ddddk")]
    [InlineData("dddk")]
    public void SimpleColours_AreAllowedInMuzukashii(string colours) =>
        Assert.Empty(RunColours("Muzukashii", Pattern(1000, colours)));

    [Theory]
    [InlineData("ddkk")] // In the middle.
    [InlineData("ddkkk")]
    [InlineData("kdddk")] // Both at the start and the end.
    [InlineData("dkdd")]
    public void ComplexColours_AreReportedInMuzukashii(string colours)
    {
        var issue = Assert.Single(RunColours("Muzukashii", Pattern(1000, colours)));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains($"pattern of {colours.Length} notes ({colours})", issue.message);
    }

    [Fact]
    public void ComplexColoursInMuzukashii_AreNotReportedInOni() =>
        Assert.Empty(RunColours("Oni", Pattern(1000, "kdddk")));

    [Fact]
    public void ComplexColoursAtHalfBeats_AreAllowed() =>
        Assert.Empty(RunColours("Muzukashii", Pattern(1000, "kdkdk", Beat / 2)));

    [Fact]
    public void HalfBeatsAt300Bpm_CountAsQuarters() =>
        Assert.Single(
            RunColours("Muzukashii", Pattern(1000, "ddkkk"), TestTimingPoints.Uninherited(0, 200))
        );

    [Fact]
    public void Drumroll_EndsPattern()
    {
        string[] objects =
        [
            .. Pattern(1000, "dd"),
            TestHitObjects.Slider(1200, length: 10),
            .. Pattern(1300, "kk"),
        ];

        Assert.Empty(RunColours("Muzukashii", objects));
    }

    [Fact]
    public void DifficultyWithoutLevelInName_IsSkipped()
    {
        using var context = Create("Custom Name", Pattern(1000, "kdddk"));

        Assert.Empty(context.RunBeatmapCheck<CheckPatternColours>("Custom Name"));
    }

    [Fact]
    public void ColouredPatternsWithinABeat_AreReportedInMuzukashii()
    {
        string[] objects = [.. Pattern(1000, "ddk"), .. Pattern(1200 + Beat, "kkd")];

        var issue = Assert.Single(RunColours("Muzukashii", objects));

        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("(ddk, kkd) in a row", issue.message);
    }

    [Fact]
    public void ColouredPatternsFurtherApart_AreAllowed() =>
        Assert.Empty(
            RunColours(
                "Muzukashii",
                [.. Pattern(1000, "ddk"), .. Pattern(1200 + Beat * 3 / 2, "kkd")]
            )
        );

    [Fact]
    public void TenColouredPatterns_AreSparse() =>
        Assert.Empty(RunColours("Muzukashii", Repeat("ddk", 10)));

    [Fact]
    public void ElevenColouredPatterns_AreReported()
    {
        var issue = Assert.Single(RunColours("Muzukashii", Repeat("ddk", 11)));

        Assert.Contains("11 of 11 1/4 patterns have colour changes", issue.message);
    }

    [Fact]
    public void ColouredPatterns_AreSparseAmongManyPlainOnes()
    {
        string[] objects = [.. Repeat("ddk", 11), .. Repeat("ddd", 33, 100_000)];

        Assert.Empty(RunColours("Muzukashii", objects));
    }

    [Theory]
    [InlineData("kdkdk")] // Not longer than five notes.
    [InlineData("kkddkkd")]
    [InlineData("ddkkddkkd")]
    [InlineData("kdkddd")] // Exactly half.
    public void SimpleColours_AreAllowedInOni(string colours) =>
        Assert.Empty(RunColours("Oni", Pattern(1000, colours)));

    [Theory]
    [InlineData("kdkkdkd", 5)]
    [InlineData("kddkddk", 4)]
    [InlineData("ddkdkkddk", 5)]
    public void ComplicatedColours_AreReportedInOni(string colours, int changes)
    {
        var issue = Assert.Single(RunColours("Oni", Pattern(1000, colours)));

        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains($"({colours}) has {changes} colour changes", issue.message);
    }

    [Theory]
    [InlineData(-80, "1.25x")]
    [InlineData(-125, "0.8x")]
    public void AbruptScrollSpeedChange_IsReported(double beatLength, string change)
    {
        var issue = Assert.Single(RunScrollSpeed(TestTimingPoints.Inherited(1200, beatLength)));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains($"changes to {change}", issue.message);
    }

    [Fact]
    public void SmoothScrollSpeedChanges_AreAllowed() =>
        Assert.Empty(
            RunScrollSpeed(
                TestTimingPoints.Inherited(1100, -90),
                TestTimingPoints.Inherited(1200, -80),
                TestTimingPoints.Inherited(1300, -70)
            )
        );

    [Fact]
    public void ScrollSpeedChangeOnLastNote_IsAllowed() =>
        Assert.Empty(RunScrollSpeed(TestTimingPoints.Inherited(1400, -50)));

    [Fact]
    public void ScrollSpeedChangeInSeparatedPattern_IsAllowed() =>
        // At 2x, 1/4 notes are 70 apart and don't overlap.
        Assert.Empty(
            RunScrollSpeed(
                TestTimingPoints.Inherited(0, -50),
                TestTimingPoints.Inherited(1200, -33)
            )
        );

    [Fact]
    public void SliderVelocityEveningOutBpm_IsAllowed() =>
        Assert.Empty(
            RunScrollSpeed(
                TestTimingPoints.Uninherited(1200, 200),
                TestTimingPoints.Inherited(1200, -200)
            )
        );

    [Fact]
    public void ScrollSpeedChangeBetweenSlowerNotes_IsAllowed() =>
        Assert.Empty(
            RunScrollSpeed(Pattern(1000, "ddddd", 200), TestTimingPoints.Inherited(1400, -50))
        );

    private static List<Issue> RunScrollSpeed(params string[] timingPoints) =>
        RunScrollSpeed(Pattern(1000, "ddddd"), timingPoints);

    private static List<Issue> RunScrollSpeed(
        IEnumerable<string> objects,
        params string[] timingPoints
    )
    {
        using var context = Create(
            "Custom Name",
            objects,
            [TestTimingPoints.Uninherited(0, Beat), .. timingPoints]
        );

        return context.RunBeatmapCheck<CheckPatternScrollSpeed>("Custom Name");
    }

    private static List<Issue> RunColours(
        string name,
        IEnumerable<string> objects,
        string? timing = null
    )
    {
        using var context = Create(name, objects, timing ?? TestTimingPoints.Uninherited(0, Beat));
        var difficulty = name == "Oni" ? Beatmap.Difficulty.Insane : Beatmap.Difficulty.Hard;
        var check = new CheckPatternColours();

        return check
            .GetIssues(context.GetBeatmap(name))
            .Where(issue => issue.WithOrigin(check).AppliesToDifficulty(difficulty))
            .ToList();
    }

    private static CheckTestContext Create(
        string name,
        IEnumerable<string> objects,
        params string[] timingPoints
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .Version(name)
                .TimingPoints(
                    timingPoints.Length > 0 ? timingPoints : [TestTimingPoints.Uninherited(0, Beat)]
                )
                .HitObjects(objects)
        );

    private static IEnumerable<string> Pattern(int start, string colours, int gap = Quarter) =>
        colours.Select(
            (colour, i) =>
                TestHitObjects.Circle(start + i * gap, hitSound: colour == 'k' ? Clap : 0)
        );

    private static IEnumerable<string> Repeat(string colours, int count, int start = 1000) =>
        Enumerable.Range(0, count).SelectMany(i => Pattern(start + i * Beat * 4, colours));
}
