using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckHardRockPerfectStacksBasicTests
{
    private static List<Issue> Run(float circleSize, params string[] hitObjects)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().CircleSize(circleSize).AR(9).WithDefaultTiming().HitObjects(hitObjects)
        );

        return context.RunBeatmapCheck<CheckHardRockPerfectStacks>("Test");
    }

    [Fact]
    public void SpreadOutObjects_AreFine()
    {
        Assert.Empty(Run(4, TestHitObjects.Circle(1000, 100), TestHitObjects.Circle(1200, 200)));
        Assert.Empty(Run(4));
    }

    [Fact]
    public void RegularStack_IsFine()
    {
        Assert.Empty(Run(4, TestHitObjects.Circle(1000, 100), TestHitObjects.Circle(1100, 100)));
    }

    [Fact]
    public void StacksOnSliderTails_AreFine()
    {
        Assert.Empty(
            Run(
                4,
                TestHitObjects.Slider(1000, "L", "300:100", 1, 70, 100, 100),
                TestHitObjects.Circle(1300, 170, 100)
            )
        );
    }

    [Fact]
    public void SpinnersBetween_AreIgnored()
    {
        Assert.Empty(
            Run(
                4,
                TestHitObjects.Circle(1000, 100),
                TestHitObjects.Spinner(1100, 1200),
                TestHitObjects.Circle(1250, 100)
            )
        );
    }

    [Fact]
    public void AlmostStacked_OutsideOfTheStackTime_IsAFailedStack()
    {
        var issue = Assert.Single(
            Run(
                4,
                TestHitObjects.Circle(1000, 100),
                TestHitObjects.Circle(1350, 101),
                TestHitObjects.Circle(1700, 102)
            )
        );

        // The stack time is 315 ms with Hard Rock.
        Assert.Contains("00:01:000 (1,2", issue.message);
    }

    [Fact]
    public void FurtherApartThanAFailedStack_IsFine()
    {
        Assert.Empty(Run(4, TestHitObjects.Circle(1000, 100), TestHitObjects.Circle(1350, 110)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void StacksAroundSliders_AreFine(int slides)
    {
        var endX = slides == 1 ? 170 : 100;

        // On the end of the slider right away, and spread out after it.
        Assert.Empty(
            Run(
                4,
                TestHitObjects.Slider(1000, "L", "300:100", slides, 70, 100, 100),
                TestHitObjects.Circle(1000 + 250 * slides + 10, endX, 100),
                TestHitObjects.Circle(1000 + 250 * slides + 400, 400, 300)
            )
        );
    }
}
