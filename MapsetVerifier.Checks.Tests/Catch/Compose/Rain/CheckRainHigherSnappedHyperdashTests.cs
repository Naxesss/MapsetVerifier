using MapsetVerifier.Checks.Catch.Compose.Rain;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose.Rain;

public class CheckRainHigherSnappedHyperdashTests
{
    private const CatchMovementType Walk = CatchMovementType.Walk;
    private const CatchMovementType Dash = CatchMovementType.Dash;
    private const CatchMovementType Hyper = CatchMovementType.Hyperdash;

    private static List<Issue> Run(int[] gaps, int[] xs, params CatchMovementType[] movements)
    {
        var time = 1000;
        var hitObjects = new List<string> { TestHitObjects.Circle(time, xs[0]) };
        for (var i = 0; i < gaps.Length; ++i)
            hitObjects.Add(TestHitObjects.Circle(time += gaps[i], xs[i + 1]));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Mode(Beatmap.Mode.Catch).WithDefaultTiming().HitObjects(hitObjects)
        );
        CatchObjects.Set(context, movements);

        return context.RunBeatmapCheck<CheckRainHigherSnappedHyperdash>("Test");
    }

    [Fact]
    public void OnItsOwn_OrBasicSnapped_IsFine()
    {
        Assert.Empty(Run([100, 300], [0, 300, 0], Hyper, Walk));
        Assert.Empty(Run([200, 100], [0, 300, 0], Hyper, Dash));
    }

    [Fact]
    public void FollowedByAHigherSnappedDash_IsMinor()
    {
        var issue = Assert.Single(Run([100, 100, 1000], [0, 300, 400, 400], Hyper, Dash));

        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("followed by a higher-snapped dash", issue.message);
        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Insane));
    }

    [Fact]
    public void FollowedByABasicSnappedDash_OnlyMattersTheOtherWay()
    {
        Assert.Empty(Run([100, 200, 1000], [0, 300, 400, 400], Hyper, Dash));
        Assert.Contains(
            "followed by an antiflow dash",
            Assert.Single(Run([100, 200, 1000], [0, 300, 100, 100], Hyper, Dash)).message
        );
    }

    [Fact]
    public void AfterAHigherSnappedDash_OnlyMattersTheOtherWay()
    {
        Assert.Empty(Run([100, 100, 1000], [0, 100, 400, 400], Dash, Hyper));
        Assert.Contains(
            "after a higher-snapped dash going the other way",
            Assert.Single(Run([100, 100, 1000], [300, 400, 100, 100], Dash, Hyper)).message
        );
    }

    [Fact]
    public void WithoutAHigherSnappedDashAround_IsFine()
    {
        // The last ones of a map.
        Assert.Empty(Run([100], [0, 300], Hyper));
        // Other hyperdashes are up to another check.
        Assert.Empty(Run([100, 100, 1000], [0, 300, 0, 0], Hyper, Hyper));
        Assert.Empty(Run([200, 100, 1000], [300, 400, 100, 100], Dash, Hyper));
    }

    [Fact]
    public void WithinASlider_IsReported()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, "L", "380:100", 4, 25, 0, 100),
                    TestHitObjects.Circle(5000, 0)
                )
        );
        CatchObjects.Set(context);
        var slider = (JuiceStream)context.GetBeatmap("Test").HitObjects[0];
        foreach (var part in slider.Parts)
            part.MovementType = Walk;
        slider.Parts[0].MovementType = Hyper;

        Assert.Single(context.RunBeatmapCheck<CheckRainHigherSnappedHyperdash>("Test"));
    }
}
