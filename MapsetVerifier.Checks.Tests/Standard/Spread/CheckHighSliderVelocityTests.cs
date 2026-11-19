using MapsetVerifier.Checks.Standard.Spread;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckHighSliderVelocityTests
{
    [Fact]
    public void BaseVelocityAtLimit_ProducesNoIssues()
    {
        using var context = CreateContext(1.3f);

        Assert.Empty(context.RunBeatmapCheck<CheckHighSliderVelocity>("Test"));
    }

    [Fact]
    public void BaseVelocityAboveLimit_ReportsOncePerLine()
    {
        using var context = CreateContext(1.4f);

        var issue = Assert.Single(context.RunBeatmapCheck<CheckHighSliderVelocity>("Test"));
        Assert.Contains("Slider velocity is 1.4x", issue.message);
    }

    [Fact]
    public void GreenLineSlowingDown_ProducesNoIssues()
    {
        // 1.4 base velocity under a 0.5x green line plays at 0.7x.
        using var context = CreateContext(1.4f, TestTimingPoints.Inherited(0, -200));

        Assert.Empty(context.RunBeatmapCheck<CheckHighSliderVelocity>("Test"));
    }

    [Fact]
    public void GreenLineSpeedingUp_IsReported()
    {
        // 1.0 base velocity under a 1.5x green line plays at 1.5x.
        using var context = CreateContext(1.0f, TestTimingPoints.Inherited(1500, -66.666666666667));

        var issue = Assert.Single(context.RunBeatmapCheck<CheckHighSliderVelocity>("Test"));
        Assert.Contains("Slider velocity is 1.5x", issue.message);
    }

    private static CheckTestContext CreateContext(float sliderMultiplier, string? greenLine = null)
    {
        var timingPoints = new List<string> { OsuBuilder.DefaultTimingPoint };
        if (greenLine != null)
            timingPoints.Add(greenLine);

        return CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .SliderMultiplier(sliderMultiplier)
                .TimingPoints(timingPoints)
                .HitObjects(
                    TestHitObjects.Slider(1000),
                    TestHitObjects.Slider(2000),
                    TestHitObjects.Slider(3000)
                )
        );
    }
}
