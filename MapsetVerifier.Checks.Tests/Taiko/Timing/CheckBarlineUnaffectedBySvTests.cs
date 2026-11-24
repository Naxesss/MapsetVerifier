using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Timing;

public class CheckBarlineUnaffectedBySvTests
{
    // Barlines every 2000 ms.
    private static List<Issue> Run(double lineOffset)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(
                    TestTimingPoints.Uninherited(0, 500),
                    TestTimingPoints.Inherited(lineOffset, -50)
                )
                .WithDefaultHitObject()
        );

        return context.RunBeatmapCheck<CheckBarlineUnaffectedBySv>("Test");
    }

    [Theory]
    [InlineData(4000)]
    [InlineData(3990)]
    [InlineData(5000)]
    public void SvChange_OnOrAwayFromABarline_IsFine(double offset)
    {
        Assert.Empty(Run(offset));
    }

    [Fact]
    public void SvChange_RightAfterABarline_IsAWarning()
    {
        // The barline at 4000 is snapped 3 ms before the line which should have modified it.
        var issue = Assert.Single(Run(4003));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("3 ms before", issue.message);
    }

    [Fact]
    public void SvChange_BarelyBeforeABarline_MayBeRoundedPastIt()
    {
        // As in Hivie's Method: the barline is a hair after 70025.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(
                    "25,416.666666666667,4,2,0,100,1,0",
                    TestTimingPoints.Inherited(70025, -50)
                )
                .WithDefaultHitObject()
        );

        Assert.Contains(
            "rounding error",
            Assert.Single(context.RunBeatmapCheck<CheckBarlineUnaffectedBySv>("Test")).message
        );
    }

    [Theory]
    // 120 BPM.
    [InlineData(-99.5, 0)] // 0.6 more.
    [InlineData(-100.5, 0)]
    [InlineData(-99, 1)] // 1.2 more.
    [InlineData(-101, 1)]
    public void SvChange_TooSmallToSee_IsFine(double beatLength, int expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(
                    TestTimingPoints.Uninherited(0, 500),
                    TestTimingPoints.Inherited(4003, beatLength)
                )
                .WithDefaultHitObject()
        );

        Assert.Equal(expected, context.RunBeatmapCheck<CheckBarlineUnaffectedBySv>("Test").Count);
    }

    [Theory]
    [InlineData("4000,-200,4,2,0,100,0,0", 4001, 0)] // As in Everlasting.
    [InlineData("3999.5,-200,4,2,0,100,0,0", 4001, 0)]
    [InlineData("3990,-200,4,2,0,100,0,0", 4001, 1)] // Not on the barline.
    [InlineData("4000,-100,4,2,0,100,0,0", 4001, 1)] // Doesn't change anything.
    public void Barline_WithALineOfItsOwn_IsFine(string line, double next, int expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(
                    TestTimingPoints.Uninherited(0, 500),
                    line,
                    TestTimingPoints.Inherited(next, -50)
                )
                .WithDefaultHitObject()
        );

        Assert.Equal(expected, context.RunBeatmapCheck<CheckBarlineUnaffectedBySv>("Test").Count);
    }
}
