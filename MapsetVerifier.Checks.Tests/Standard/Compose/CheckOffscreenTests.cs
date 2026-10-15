using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckOffscreenTests
{
    // At CS 5 the circle radius is exactly 32, so with the lower limit at 428 an object is
    // offscreen from y 397 onwards, and borderline at y 396.
    private static CheckTestContext CreateContext(params string[] hitObjects) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Offscreen")
                .CircleSize(5)
                .WithDefaultTiming()
                .HitObjects(hitObjects.ToList())
        );

    [Fact]
    public void CircleWellInsideScreen_DoesNotFlag()
    {
        using var context = CreateContext(TestHitObjects.Circle(1000, y: 340));

        Assert.Empty(context.RunBeatmapCheck<CheckOffscreen>("Test"));
    }

    [Fact]
    public void CircleOffscreenAtBottom_FlagsProblem()
    {
        using var context = CreateContext(TestHitObjects.Circle(1000, y: 397));

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOffscreen>("Test"));
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("Circle is offscreen", issue.message);
    }

    [Fact]
    public void CircleTouchingBottomEdge_FlagsBorderlineWarning()
    {
        using var context = CreateContext(TestHitObjects.Circle(1000, y: 396));

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOffscreen>("Test"));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("Circle is only 0 px away from being offscreen", issue.message);
    }

    [Fact]
    public void CircleJustOutsideBorderlineMargin_DoesNotFlag()
    {
        using var context = CreateContext(TestHitObjects.Circle(1000, y: 395));

        Assert.Empty(context.RunBeatmapCheck<CheckOffscreen>("Test"));
    }

    [Fact]
    public void CircleHalfAPixelFromEdge_ReportsRemainingMargin()
    {
        // Not expressible through integer positions, so the head is placed a pixel higher and
        // the tail of a linear slider is used to land half a pixel away from the edge instead.
        using var context = CreateContext(
            TestHitObjects.Slider(1000, curvePoints: "256:396", length: 95.5, y: 300)
        );

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOffscreen>("Test"));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("Slider tail is only 0.5 px away from being offscreen", issue.message);
    }

    [Fact]
    public void SliderTailOffscreenAtBottom_FlagsProblem()
    {
        using var context = CreateContext(
            TestHitObjects.Slider(1000, curvePoints: "256:400", length: 100, y: 300)
        );

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOffscreen>("Test"));
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("Slider tail is offscreen", issue.message);
    }

    [Fact]
    public void SliderTailTouchingBottomEdge_FlagsBorderlineWarningOnce()
    {
        using var context = CreateContext(
            TestHitObjects.Slider(1000, curvePoints: "256:396", length: 96, y: 300)
        );

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOffscreen>("Test"));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("Slider tail is only 0 px away from being offscreen", issue.message);
    }

    [Fact]
    public void SliderTailTouchingRightEdge_FlagsBorderlineWarning()
    {
        // The right limit is 579, so with a radius of 32 the body reaches the edge at x 547,
        // which is outside the playfield and therefore not moved back in by the game.
        using var context = CreateContext(
            TestHitObjects.Slider(1000, curvePoints: "547:192", length: 291, x: 256)
        );

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOffscreen>("Test"));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("Slider tail is only 0 px away from being offscreen", issue.message);
    }

    [Fact]
    public void SliderBodyTouchingBottomEdge_FlagsBorderlineWarning()
    {
        // Reverses back to the head, so only the body itself comes close to the edge.
        using var context = CreateContext(
            TestHitObjects.Slider(1000, curvePoints: "256:396", slides: 2, length: 96, y: 300)
        );

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOffscreen>("Test"));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("Slider body is only 0 px away from being offscreen", issue.message);
    }

    [Fact]
    public void BezierSliderWithRedAnchorNearTopEdge_FlagsBorderlineWarning()
    {
        // The red anchor at 144:-23 is the topmost point of the path and leaves 0.52 px at CS 4,
        // which the path sampling used to cut the corner of, reporting several pixels of margin.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Offscreen")
                .CircleSize(4)
                .SliderMultiplier(2.4f)
                .WithDefaultTiming()
                .HitObjects("150,39,1000,6,0,B|144:-23|144:-23|173:105,1,180,0|0,0:0|0:0,0:0:0:0:")
        );

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOffscreen>("Test"));
        Assert.Equal(Issue.Level.Warning, issue.level);
        // The exact figure follows the sampling, which lands a fraction of a pixel off the anchor.
        Assert.Contains("Slider body is only 0.", issue.message);
    }

    [Fact]
    public void SliderWellInsideScreen_DoesNotFlag()
    {
        using var context = CreateContext(
            TestHitObjects.Slider(1000, curvePoints: "256:340", length: 100, y: 240)
        );

        Assert.Empty(context.RunBeatmapCheck<CheckOffscreen>("Test"));
    }
}
