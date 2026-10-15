using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckSliderOnlySectionsTests
{
    [Fact]
    public void LongSliderOnlySection_FlagsMinor()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 7000; time += 1000)
            hitObjects.Add(TestHitObjects.Slider(time));
        hitObjects.Add(TestHitObjects.Circle(9000));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Slider Only Sections")
                .WithDefaultTiming()
                .HitObjects(hitObjects)
        );

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("8 objects", issue.message);
    }

    [Fact]
    public void TrailingSliderOnlySection_WithNoFollowingObject_FlagsMinor()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 7000; time += 1000)
            hitObjects.Add(TestHitObjects.Slider(time));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Slider Only Sections")
                .WithDefaultTiming()
                .HitObjects(hitObjects)
        );

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        Assert.Single(issues);
    }

    [Fact]
    public void FewSliders_DoesNotFlag()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 4000; time += 1000)
            hitObjects.Add(TestHitObjects.Slider(time));
        hitObjects.Add(TestHitObjects.Circle(6000));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Slider Only Sections")
                .WithDefaultTiming()
                .HitObjects(hitObjects)
        );

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void ManySliders_ButShortDuration_DoesNotFlag()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 700; time += 100)
            hitObjects.Add(TestHitObjects.Slider(time));
        hitObjects.Add(TestHitObjects.Circle(1000));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Slider Only Sections")
                .WithDefaultTiming()
                .HitObjects(hitObjects)
        );

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void CircleBreaksUpSections_NeitherSectionFlags()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 3000; time += 1000)
            hitObjects.Add(TestHitObjects.Slider(time));
        hitObjects.Add(TestHitObjects.Circle(4000));
        for (var time = 5000; time <= 8000; time += 1000)
            hitObjects.Add(TestHitObjects.Slider(time));
        hitObjects.Add(TestHitObjects.Circle(9000));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Slider Only Sections")
                .WithDefaultTiming()
                .HitObjects(hitObjects)
        );

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        Assert.Empty(issues);
    }
}
