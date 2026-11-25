using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class SliderVelocityTests
{
    /// <summary> Over a minute, with the given number of sv changes. </summary>
    private static CheckTestContext Changing(int changes) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .WithDefaultTiming()
                .TimingPoints(
                    Enumerable
                        .Range(1, changes)
                        .Select(i => TestTimingPoints.Inherited(i * 2000, i % 2 == 1 ? -50 : -100))
                )
                .HitObjects(
                    Enumerable
                        .Range(0, changes + 1)
                        .Select(i => TestHitObjects.Slider(i * 2000, length: 70))
                )
                .HitObjects(TestHitObjects.Circle(60000))
        );

    [Theory]
    [InlineData(8, false, false)]
    [InlineData(9, true, false)]
    [InlineData(12, true, false)]
    [InlineData(13, true, true)]
    public void FrequentChanges_PerDifficulty(int changes, bool easy, bool normal)
    {
        using var context = Changing(changes);
        var issues = context.RunBeatmapCheck<CheckSliderVelocityChanges>("Test");

        Assert.Equal(easy, issues.Any(i => i.AppliesToDifficulty(Beatmap.Difficulty.Easy)));
        Assert.Equal(normal, issues.Any(i => i.AppliesToDifficulty(Beatmap.Difficulty.Normal)));
    }

    [Fact]
    public void HighVelocity_IsMentionedOncePerLine()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .SliderMultiplier(3.6f)
                .TimingPoints(
                    TestTimingPoints.Uninherited(0, 500),
                    TestTimingPoints.Inherited(0, -10),
                    TestTimingPoints.Inherited(9000, -10)
                )
                .HitObjects(
                    TestHitObjects.Slider(1000),
                    TestHitObjects.Slider(2000),
                    TestHitObjects.Slider(10000)
                )
        );

        Assert.Equal(2, context.RunBeatmapCheck<CheckHighSliderVelocity>("Test").Count);
    }
}
