using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko;

public class TaikoUtilsTests
{
    // Barlines every 2000 ms, and every 1500 ms from 9000 on.
    private static readonly string[] Lines =
    [
        TestTimingPoints.Uninherited(1000, 500),
        TestTimingPoints.Uninherited(9000, 500, meter: 3, effects: 8),
    ];

    private static Beatmap Map()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(Lines)
                .HitObjects(TestHitObjects.Circle(1000))
        );

        return context.GetBeatmap("Test");
    }

    [Theory]
    [InlineData(1000, 0)]
    [InlineData(1003, 3)]
    [InlineData(2999, -1)]
    [InlineData(3000, 0)]
    [InlineData(8998, -2)] // The next red line has a barline of its own,
    [InlineData(10499, -1)] // and its own meter.
    [InlineData(10502, 2)]
    public void NearestBarline_IsSigned(double time, double expected)
    {
        Assert.Equal(expected, TaikoUtils.GetOffsetFromNearestBarlineMs(Map(), time), 6);
    }

    [Theory]
    [InlineData(1000, 2000)]
    [InlineData(1500, 1500)]
    [InlineData(2999, 1)]
    public void UntilTheNextBarline(double time, double expected)
    {
        Assert.Equal(expected, TaikoUtils.GetMsUntilNextBarline(Map(), time), 6);
        Assert.Equal(-expected, TaikoUtils.GetOffsetFromNextBarlineMs(Map(), time), 6);
    }

    [Theory]
    [InlineData(3000, true)]
    [InlineData(3002, true)]
    [InlineData(2998, true)]
    [InlineData(3004, false)]
    [InlineData(2500, false)]
    public void OnABarline_WithinSomeLeniency(double time, bool expected)
    {
        Assert.Equal(expected, TaikoUtils.IsOnBarline(Map(), time));
    }

    [Theory]
    [InlineData(3000, true)]
    [InlineData(9000, false)] // Omitted.
    [InlineData(10500, true)] // Only the first is.
    [InlineData(2500, false)]
    public void VisibleBarline(double time, bool expected)
    {
        Assert.Equal(expected, TaikoUtils.IsBarlineVisible(Map(), time));
    }

    [Theory]
    [InlineData(500, 500 / 1.5)] // 120 bpm counts as 180.
    [InlineData(375, 375)] // 160 bpm as it is.
    [InlineData(200, 400)] // 300 bpm as 150.
    [InlineData(600, 300)] // 100 bpm as 200.
    public void NormalisedBeatLength(double msPerBeat, double expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .TimingPoints(TestTimingPoints.Uninherited(0, msPerBeat))
                .WithDefaultHitObject()
        );
        var line = (UninheritedLine)context.GetBeatmap("Test").TimingLines[0];

        Assert.Equal(expected, line.GetNormalizedMsPerBeat(), 6);
    }

    [Theory]
    [InlineData(-3, 5, -3, 5)]
    [InlineData(3, -5, 3, -5)]
    public void LowerAndHigherAbsoluteValue_KeepTheirSign(
        double first,
        double second,
        double lower,
        double higher
    )
    {
        Assert.Equal(lower, GeneralUtils.TakeLowerAbsValue(first, second));
        Assert.Equal(higher, GeneralUtils.TakeHigherAbsValue(first, second));
    }

    [Fact]
    public void WithoutRedLines_ThereAreNoBarlines()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(TestTimingPoints.Inherited(0, -100))
                .HitObjects(TestHitObjects.Circle(1000))
        );
        var map = context.GetBeatmap("Test");

        Assert.Equal(0, TaikoUtils.GetOffsetFromPrevBarlineMs(map, 1000));
        Assert.Equal(0, TaikoUtils.GetOffsetFromNextBarlineMs(map, 1000));
        Assert.Equal(0, TaikoUtils.GetMsUntilNextBarline(map, 1000));
        Assert.False(TaikoUtils.IsOnBarline(map, 1000));
    }

    [Theory]
    [InlineData(new[] { 1000, 1125, 1250 }, 0, 125d)]
    [InlineData(new[] { 1000, 1125, 1250 }, 2, 125d)]
    [InlineData(new[] { 1000, 1125, 1200 }, 1, 75d)]
    public void PatternSpacing_IsTheSmallerGap(int[] times, int index, double expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .WithDefaultTiming()
                .HitObjects(times.Select(time => TestHitObjects.Circle(time)))
        );

        Assert.Equal(expected, context.GetBeatmap("Test").HitObjects[index].GetPatternSpacingMs());
    }

    [Fact]
    public void GeneralHelpers()
    {
        Assert.True(5d.IsWithin(1, 4));
        Assert.True(3d.IsWithin(1, 4));
        Assert.False(2.9.IsWithin(1, 4));
        Assert.False(5.1.IsWithin(1, 4));

        List<string> list = ["a", "b"];
        Assert.Equal("b", list.SafeGetIndex(1));
        Assert.Null(list.SafeGetIndex(2));
        Assert.Null(list.SafeGetIndex(-1));
    }
}
