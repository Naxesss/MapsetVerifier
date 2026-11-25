using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckObscuredReverseTests
{
    private static List<Issue> Run(int x, int y, int time = 1300, int slides = 2)
    {
        // One beat to the right, so the reverse is at (240, 100) and 1500 ms.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .CircleSize(4)
                .AR(9)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, "L", "300:100", slides, 140, 100, 100),
                    TestHitObjects.Circle(time, x, y)
                )
        );

        return context.RunBeatmapCheck<CheckObscuredReverse>("Test");
    }

    [Fact]
    public void CircleOnTheReverse_IsAWarning()
    {
        var issue = Assert.Single(Run(240, 100));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("Reverse arrow", issue.message);
        Assert.DoesNotContain("potentially", issue.message);
    }

    [Fact]
    public void CircleNextToTheReverse_IsPotentiallyObscuring()
    {
        // The radius is 36.48, so between a third and 1/1.75 of it.
        Assert.Contains("potentially", Assert.Single(Run(256, 100)).message);
    }

    [Fact]
    public void FarAway_OrAfterTheReverse_OrWithoutOne_IsFine()
    {
        Assert.Empty(Run(240, 130));
        Assert.Empty(Run(240, 100, time: 1501));
        Assert.Empty(Run(240, 100, slides: 1));
    }

    /// <summary> What counts of a slider is where it ends, not where it starts. </summary>
    [Theory]
    [InlineData(240, 440, 1)]
    [InlineData(440, 240, 0)]
    public void SliderEndingOnTheReverse_Counts(int end, int start, int expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .CircleSize(4)
                .AR(9)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(600, "L", $"{end}:100", 1, 200, start, 100),
                    TestHitObjects.Slider(1000, "L", "300:100", 2, 140, 100, 100)
                )
        );

        Assert.Equal(expected, context.RunBeatmapCheck<CheckObscuredReverse>("Test").Count);
    }
}
