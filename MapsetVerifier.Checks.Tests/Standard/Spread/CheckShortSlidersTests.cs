using MapsetVerifier.Checks.Standard.Spread;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckShortSlidersTests
{
    [Theory]
    [InlineData(35, false)] // 125 ms
    [InlineData(28, true)] // 100 ms
    public void ShorterThan125Ms_IsAWarning(double length, bool expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .WithDefaultTiming()
                .HitObjects(TestHitObjects.Slider(1000, length: length))
        );

        var issues = context.RunBeatmapCheck<CheckShortSliders>("Test");

        Assert.Equal(expected, issues.Count == 1);
        Assert.All(
            issues,
            issue => Assert.Contains("100 ms, expected at least 125", issue.message)
        );
    }
}
