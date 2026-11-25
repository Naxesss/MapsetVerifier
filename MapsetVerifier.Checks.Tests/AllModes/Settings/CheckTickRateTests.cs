using MapsetVerifier.Checks.AllModes.Settings;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Settings;

public class CheckTickRateTests
{
    [Theory]
    [InlineData(0.5f, false)]
    [InlineData(1f, false)]
    [InlineData(1.333f, false)]
    [InlineData(1.33333333f, false)]
    [InlineData(1.5f, false)]
    [InlineData(2f, false)]
    [InlineData(4f, false)]
    [InlineData(1.25f, true)]
    [InlineData(1.2f, true)]
    [InlineData(2.5f, true)]
    public void UncommonTickRate_IsAProblem(float tickRate, bool expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().SliderTickRate(tickRate).WithDefaultTiming().WithDefaultHitObject()
        );

        var issues = context.RunBeatmapCheck<CheckTickRate>("Test");

        Assert.Equal(expected, issues.Count == 1);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
    }
}
