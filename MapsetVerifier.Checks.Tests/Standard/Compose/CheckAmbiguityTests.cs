using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckAmbiguityTests
{
    private static List<Issue> Run(string curve, string points, double length, float tickRate = 1)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .CircleSize(6)
                .SliderTickRate(tickRate)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, curve, points, length: length, x: 100, y: 100)
                )
        );

        return context.RunBeatmapCheck<CheckAmbiguity>("Test");
    }

    [Fact]
    public void StraightSlider_IsFine()
    {
        Assert.Empty(Run("L", "400:100", 300));
    }

    [Fact]
    public void TailOnTheHead_IsAWarning()
    {
        // Out, around and back as a triangle.
        var issue = Assert.Single(Run("B", "300:100|300:100|200:270|200:270|101:101", 597));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("edges are almost perfectly overlapping", issue.message);
    }

    [Fact]
    public void SmallLoop_CannotBreak()
    {
        // Never further from the head than the circle itself.
        Assert.Empty(Run("B", "110:100|110:100|105:108|105:108|101:101", 25));
    }

    [Fact]
    public void RedAnchorOnTheTail_IsAWarning()
    {
        var issue = Assert.Single(
            Run("B", "200:100|200:100|300:100|300:100|300:200|300:200|200:102", 440)
        );

        Assert.Contains("red anchor overlap", issue.message);
    }

    [Fact]
    public void RedAnchorOnTheHead_IsAWarning()
    {
        // Around and back to the head, and on from there.
        var issue = Assert.Single(
            Run("B", "200:100|200:100|200:200|200:200|102:100|102:100|50:200", 450)
        );

        Assert.Contains("Head", issue.message);
        Assert.Contains("red anchor overlap", issue.message);
    }
}
