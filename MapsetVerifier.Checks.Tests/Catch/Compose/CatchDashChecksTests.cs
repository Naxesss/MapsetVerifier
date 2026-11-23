using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

/// <summary>
///     Fruits are placed far enough apart for the movement each test needs; every test first checks that the
///     parser classifies the movements as intended, so a change in movement detection shows up here clearly.
/// </summary>
public class CatchDashChecksTests
{
    [Fact]
    public void ThreeBasicDashesInARow_AreReportedInSalad()
    {
        using var context = Create((1000, 100), (1300, 360), (1600, 100), (1900, 360), (2500, 360));
        AssertMovements(context, "DDDW");

        var issue = Assert.Single(Run<CheckDashPatterns>(context, Beatmap.Difficulty.Normal));
        Assert.Contains("3 basic-snapped dashes in a row, more than 2", issue.message);
        Assert.Equal(Issue.Level.Problem, issue.level);
    }

    [Fact]
    public void TwoBasicDashesInARow_AreFineInSalad()
    {
        using var context = Create((1000, 100), (1300, 360), (1600, 100), (2600, 100));
        AssertMovements(context, "DDW");

        Assert.Empty(Run<CheckDashPatterns>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void ShortDash_IsReportedInSalad()
    {
        using var context = Create((1000, 100), (1100, 210), (2000, 210));
        AssertMovements(context, "DW");

        var issue = Assert.Single(Run<CheckDashPatterns>(context, Beatmap.Difficulty.Normal));
        Assert.Contains("Dash of 100 ms, less than 125 ms", issue.message);
    }

    [Fact]
    public void HigherSnappedDashFollowedByDash_IsReportedInSalad()
    {
        using var context = Create((1000, 100), (1200, 285), (1500, 30), (2500, 30));
        AssertMovements(context, "DDW");

        Assert.Contains(
            Run<CheckDashPatterns>(context, Beatmap.Difficulty.Normal),
            issue => issue.message.Contains("Higher-snapped dash not followed by a walk")
        );
    }

    [Fact]
    public void HyperdashesOfDifferentSnaps_AreReportedInPlatter()
    {
        using var context = Create((1000, 16), (1400, 496), (1600, 196), (2600, 196));
        AssertMovements(context, "HHW");

        Assert.Contains(
            Run<CheckDashPatterns>(context, Beatmap.Difficulty.Hard),
            issue => issue.message.Contains("Hyperdashes of different snaps in a row")
        );
    }

    [Fact]
    public void DashToTheBorder_IsReported()
    {
        using var context = Create((1000, 250), (1300, 508), (2300, 508));
        AssertMovements(context, "DW");

        var issue = Assert.Single(Run<CheckDashToBorder>(context, Beatmap.Difficulty.Normal));
        Assert.Contains("Dash lands 4 px from the border", issue.message);
    }

    [Fact]
    public void SmallStepBack_IsNotAntiflow()
    {
        using var context = Create((1000, 100), (1300, 300), (1600, 290), (1900, 200));
        var objects = context.GetBeatmap("Test").GetCatchHitObjects(includeJuiceStreamParts: true);

        Assert.False(CheckDashPatterns.IsAntiflow(objects[0], objects[1], objects[2], minBack: 40));
        Assert.True(CheckDashPatterns.IsAntiflow(objects[0], objects[1], objects[3], minBack: 40));
    }

    private static void AssertMovements(CheckTestContext context, string expected)
    {
        var objects = context.GetBeatmap("Test").GetCatchHitObjects(includeJuiceStreamParts: true);
        var actual = string.Concat(
            objects
                .Take(objects.Count - 1)
                .Select(o =>
                    o.MovementType switch
                    {
                        CatchMovementType.Dash => 'D',
                        CatchMovementType.Hyperdash => 'H',
                        _ => 'W',
                    }
                )
        );

        Assert.Equal(expected, actual);
    }

    private static List<Issue> Run<TCheck>(CheckTestContext context, Beatmap.Difficulty difficulty)
        where TCheck : BeatmapCheck, new()
    {
        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = difficulty;

        return new TCheck()
            .GetIssues(beatmap)
            .Where(issue =>
                issue.InterpretationPairs.Any(pair =>
                    pair.Key == "difficulty" && pair.Value == (int)difficulty
                )
            )
            .ToList();
    }

    private static CheckTestContext Create(params (int Time, int X)[] fruits) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .TimingPoints(TestTimingPoints.Uninherited(0, 60000d / 180))
                .HitObjects(fruits.Select(fruit => TestHitObjects.Circle(fruit.Time, x: fruit.X)))
        );
}
