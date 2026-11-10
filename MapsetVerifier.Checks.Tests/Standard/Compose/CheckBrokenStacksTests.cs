using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckBrokenStacksTests
{
    private static List<Issue> Run(params string[] hitObjects)
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "stacks.osu",
                new OsuBuilder()
                    .Version("Stacks")
                    .CircleSize(4)
                    .AR(9.3f)
                    .StackLeniency(0.7f)
                    .WithDefaultTiming()
                    .HitObjects(hitObjects)
                    .Build()
            ),
        ]);

        return context.RunBeatmapCheck<CheckBrokenStacks>("Stacks");
    }

    [Fact]
    public void OneObjectSlightlyOffAStack_IsReported()
    {
        // Like IGNITE [Distortion] 01:16:698 (1,2,3): 2 and 3 stack, 1 is 10.6 px away.
        var issues = Run(
            TestHitObjects.Circle(1000, 505, 370),
            TestHitObjects.Circle(1088, 512, 378),
            TestHitObjects.Circle(1175, 512, 378)
        );

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains($"{10.63:0.##} px", issue.message);
    }

    [Fact]
    public void ObjectsExactlyThreePixelsApart_AreReported_SinceTheGameNoLongerStacksThem()
    {
        var issues = Run(
            TestHitObjects.Circle(1000, 198, 222),
            TestHitObjects.Circle(1071, 198, 222),
            TestHitObjects.Circle(1143, 201, 222)
        );

        Assert.Single(issues);
    }

    [Fact]
    public void ProperStack_IsNotReported()
    {
        var issues = Run(
            TestHitObjects.Circle(1000, 256, 192),
            TestHitObjects.Circle(1088, 256, 192),
            TestHitObjects.Circle(1175, 257, 193)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void StackThatDriftsEveryObject_IsNotReported()
    {
        // A stream moving a few pixels each object into a stack is a pattern of its own.
        var issues = Run(
            TestHitObjects.Circle(1000, 119, 323),
            TestHitObjects.Circle(1100, 123, 327),
            TestHitObjects.Circle(1200, 127, 331),
            TestHitObjects.Circle(1300, 131, 335),
            TestHitObjects.Circle(1400, 131, 335)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void NearbyObjectsThatNeverStack_AreNotReported()
    {
        var issues = Run(
            TestHitObjects.Circle(1000, 256, 192),
            TestHitObjects.Circle(1088, 264, 192)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void ObjectNearWhereASliderEnds_IsNotReported()
    {
        // Circle stacked on a slider head, the next object placed 7 px from where the slider ends (256,292).
        var issues = Run(
            TestHitObjects.Circle(1000, 256, 192),
            TestHitObjects.Slider(1088, "L", "256:300"),
            TestHitObjects.Circle(1400, 262, 296)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void StreamEndingNearALaterStack_IsNotReported()
    {
        // Like Minamo no Sakura [Karcher's Insane] 01:49:101 (8,9,1): 8 to 9 is a stream step,
        // 1 is stacked on 9 but comes four times later.
        var issues = Run(
            TestHitObjects.Circle(1000, 151, 271),
            TestHitObjects.Circle(1075, 139, 272),
            TestHitObjects.Circle(1375, 139, 272)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void ObjectsTooFarApartInTime_AreNotReported()
    {
        var issues = Run(
            TestHitObjects.Circle(1000, 505, 370),
            TestHitObjects.Circle(2000, 512, 378),
            TestHitObjects.Circle(2088, 512, 378)
        );

        Assert.Empty(issues);
    }
}
