using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CheckHasEdgeDashTests
{
    private static List<Issue> Run(float distanceToHyper, CatchMovementType movement)
    {
        // 180 bpm, where distances aren't scaled.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .TimingPoints(TestTimingPoints.Uninherited(0, 333.333333333333))
                .HitObjects(TestHitObjects.Circle(1000, 50), TestHitObjects.Circle(1400, 350))
        );
        CatchObjects.Set(context, movement);
        var first = (ICatchHitObject)context.GetBeatmap("Test").HitObjects[0];
        first.DistanceToHyper = distanceToHyper;

        return context.RunBeatmapCheck<CheckHasEdgeDash>("Test");
    }

    private static Issue.Level? Level(List<Issue> issues, Beatmap.Difficulty difficulty) =>
        issues.SingleOrDefault(issue => issue.AppliesToDifficulty(difficulty))?.level;

    [Fact]
    public void EdgeDash_IsMoreSevereTheEasierTheDifficulty()
    {
        // 10 px when there are 300 ms or more.
        var issues = Run(10, CatchMovementType.Dash);

        Assert.Equal(Issue.Level.Problem, Level(issues, Beatmap.Difficulty.Normal));
        Assert.Equal(Issue.Level.Problem, Level(issues, Beatmap.Difficulty.Hard));
        Assert.Equal(Issue.Level.Warning, Level(issues, Beatmap.Difficulty.Insane));
        Assert.Equal(Issue.Level.Minor, Level(issues, Beatmap.Difficulty.Expert));
        Assert.Null(Level(issues, Beatmap.Difficulty.Easy));
    }

    [Fact]
    public void StrongDash_IsMinor()
    {
        var issues = Run(40, CatchMovementType.Dash);

        Assert.Equal(Issue.Level.Minor, Level(issues, Beatmap.Difficulty.Normal));
        Assert.Equal(Issue.Level.Minor, Level(issues, Beatmap.Difficulty.Expert));
    }

    [Fact]
    public void FarFromAHyper_OrNoDash_IsFine()
    {
        Assert.Empty(Run(51, CatchMovementType.Dash));
        Assert.Empty(Run(5, CatchMovementType.Walk));
        Assert.Empty(Run(float.PositiveInfinity, CatchMovementType.Dash));
    }

    [Theory]
    [InlineData(0.5f, "1 pixel ")]
    [InlineData(1.5f, "2 pixels")]
    public void Distance_IsRoundedUp(float distance, string expected)
    {
        Assert.All(
            Run(distance, CatchMovementType.Dash),
            issue => Assert.Contains(expected, issue.message + " ")
        );
    }
}
