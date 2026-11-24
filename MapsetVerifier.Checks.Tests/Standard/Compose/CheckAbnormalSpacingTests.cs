using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckAbnormalSpacingTests
{
    private static List<Issue> Run(int lastDistance, int interval = 125)
    {
        // Circles 80 px apart, going back and forth, until the last jump.
        var circles = Enumerable
            .Range(0, 10)
            .Select(i => TestHitObjects.Circle(1000 + i * interval, x: i % 2 * 80, y: 50))
            .Append(TestHitObjects.Circle(1000 + 10 * interval, x: 80 + lastDistance, y: 50));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().CircleSize(5).WithDefaultTiming().HitObjects(circles)
        );

        return context.RunBeatmapCheck<CheckAbnormalSpacing>("Test");
    }

    [Theory]
    [InlineData(80, null)]
    [InlineData(150, null)] // Not twice as far.
    [InlineData(200, Issue.Level.Minor)]
    [InlineData(340, Issue.Level.Warning)]
    public void FurtherThanOtherObjectsOfThatSnap(int distance, Issue.Level? expected)
    {
        Assert.Equal(expected, Run(distance).SingleOrDefault()?.level);
    }

    [Fact]
    public void SlowPatterns_AreIgnored()
    {
        Assert.Empty(Run(340, 250));
    }

    [Fact]
    public void FarFurther_IsAProblem()
    {
        // Streams of 20 px, and then a jump across the playfield.
        var circles = Enumerable
            .Range(0, 10)
            .Select(i => TestHitObjects.Circle(1000 + i * 125, x: i % 2 * 10, y: 50))
            .Append(TestHitObjects.Circle(2250, x: 500, y: 300));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().CircleSize(5).WithDefaultTiming().HitObjects(circles)
        );

        Assert.Equal(
            Issue.Level.Problem,
            Assert.Single(context.RunBeatmapCheck<CheckAbnormalSpacing>("Test")).level
        );
    }

    [Fact]
    public void Sliders_AreComparedApart_WithLeniency()
    {
        var objects = Enumerable
            .Range(0, 8)
            .Select(i => TestHitObjects.Slider(1000 + i * 250, "L", "90:50", 1, 35, i % 2 * 40, 50))
            .Append(TestHitObjects.Spinner(3200, 4000))
            .Append(TestHitObjects.Circle(4050, x: 500));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().CircleSize(5).WithDefaultTiming().HitObjects(objects)
        );

        Assert.Empty(context.RunBeatmapCheck<CheckAbnormalSpacing>("Test"));
    }

    [Theory]
    [InlineData(160, null)] // Within the follow circle.
    [InlineData(500, Issue.Level.Problem)]
    public void SlidersFurther_AreGivenLeniency(int x, Issue.Level? expected)
    {
        // Sliders of a pixel, 250 ms each.
        var objects = Enumerable
            .Range(0, 8)
            .Select(i => TestHitObjects.Slider(1000 + i * 125, "L", "90:50", 1, 1, i % 2 * 25, 50))
            .Append(TestHitObjects.Circle(2000, x: x, y: 50));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().CircleSize(5).WithDefaultTiming().HitObjects(objects)
        );

        Assert.Equal(
            expected,
            context.RunBeatmapCheck<CheckAbnormalSpacing>("Test").SingleOrDefault()?.level
        );
    }
}
