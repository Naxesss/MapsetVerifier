using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Checks.Catch.Compose.Platter;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CatchPatternChecksTests
{
    [Theory]
    [InlineData(Beatmap.Difficulty.Normal)]
    [InlineData(Beatmap.Difficulty.Hard)]
    [InlineData(Beatmap.Difficulty.Insane)]
    public void ObjectAtTheBorderAfterSpinner_IsReported(Beatmap.Difficulty difficulty)
    {
        using var context = CreateSpinner(gap: 300, nextX: 490);

        var issue = Assert.Single(
            Run<CheckSpinnerTrap>(context, difficulty),
            issue => issue.message.Contains("from the border")
        );
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("fruit 22 px from the border after 300 ms", issue.message);
    }

    [Theory]
    [InlineData(Beatmap.Difficulty.Easy)]
    [InlineData(Beatmap.Difficulty.Expert)]
    public void SpinnerTraps_AreFineInOtherLevels(Beatmap.Difficulty difficulty)
    {
        using var context = CreateSpinner(gap: 50, nextX: 490);

        Assert.Empty(Run<CheckSpinnerTrap>(context, difficulty));
    }

    [Fact]
    public void ObjectAtTheBorderLongAfterSpinner_IsFine()
    {
        using var context = CreateSpinner(gap: 1000, nextX: 490);

        Assert.Empty(Run<CheckSpinnerTrap>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void ObjectAwayFromTheBorderAfterSpinner_IsFine()
    {
        using var context = CreateSpinner(gap: 300, nextX: 448);

        Assert.Empty(Run<CheckSpinnerTrap>(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void ObjectOutOfReachAfterTheBananas_IsReported()
    {
        // The bananas only depend on what comes before them, so moving the next fruit leaves them in place.
        using var original = CreateSpinner(gap: 50, nextX: 256);
        var bananas = GetBananas(original);
        Assert.Equal(1300, bananas[^1].Time);

        var oppositeX = bananas[^1].X < 256 ? 460 : 52;
        using var context = CreateSpinner(gap: 50, nextX: oppositeX);

        var issue = Assert.Single(Run<CheckSpinnerTrap>(context, Beatmap.Difficulty.Insane));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("px out of reach", issue.message);
    }

    [Fact]
    public void ObjectUnderTheLastBanana_IsFine()
    {
        using var original = CreateSpinner(gap: 50, nextX: 256);
        var x = (int)Math.Clamp(GetBananas(original)[^1].X, 52, 460);
        using var context = CreateSpinner(gap: 50, nextX: x);

        Assert.Empty(Run<CheckSpinnerTrap>(context, Beatmap.Difficulty.Insane));
    }

    [Fact]
    public void BananaPositions_AreTheSameEveryTime()
    {
        using var first = CreateSpinner(gap: 50, nextX: 256);
        using var second = CreateSpinner(gap: 50, nextX: 100);

        Assert.Equal(GetBananas(first), GetBananas(second));
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(1200)]
    public void HyperdashJustOverTriggerDistance_IsFine(int nextTime)
    {
        var x = 16 + (int)(GetTriggerDistance(nextTime) * 1.1);
        using var context = Create(Fruit(900, 16), Fruit(nextTime, x), Fruit(5000, x));
        AssertHyperdash(context);

        Assert.Empty(Run<CheckPlatterStrongHyperdash>(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void StrongHigherSnappedHyperdash_IsMinor()
    {
        var x = 16 + (int)(GetTriggerDistance(1050) * 1.4);
        using var context = Create(Fruit(900, 16), Fruit(1050, x), Fruit(5000, x));
        AssertHyperdash(context);

        var issue = Assert.Single(
            Run<CheckPlatterStrongHyperdash>(context, Beatmap.Difficulty.Hard)
        );
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("higher-snapped hyperdash from fruit", issue.message);
        Assert.Contains("x the trigger distance (max 1.3x)", issue.message);
    }

    [Fact]
    public void SameStrengthBasicSnappedHyperdash_IsFine()
    {
        var x = 16 + (int)(GetTriggerDistance(1160) * 1.4);
        using var context = Create(Fruit(900, 16), Fruit(1160, x), Fruit(5000, x));
        AssertHyperdash(context);

        Assert.Empty(Run<CheckPlatterStrongHyperdash>(context, Beatmap.Difficulty.Hard));
    }

    [Theory]
    [InlineData(1050)]
    [InlineData(1160)]
    public void VeryStrongHyperdash_IsWarning(int nextTime)
    {
        var x = 16 + (int)(GetTriggerDistance(nextTime) * 1.55);
        using var context = Create(Fruit(900, 16), Fruit(nextTime, x), Fruit(5000, x));
        AssertHyperdash(context);

        var issue = Assert.Single(
            Run<CheckPlatterStrongHyperdash>(context, Beatmap.Difficulty.Hard)
        );
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("(max 1.5x)", issue.message);
    }

    [Fact]
    public void StrongHyperdash_IsFineInRain()
    {
        using var context = Create(Fruit(900, 16), Fruit(1050, 496), Fruit(5000, 496));
        AssertHyperdash(context);

        Assert.Empty(Run<CheckPlatterStrongHyperdash>(context, Beatmap.Difficulty.Insane));
    }

    [Fact]
    public void GuessedLevel_IsSkipped()
    {
        using var context = Create(Fruit(900, 16), Fruit(1050, 496), Fruit(5000, 496));
        var beatmap = context.GetBeatmap("Test");
        Assert.True(beatmap.IsDifficultyGuessed());

        Assert.Empty(new CheckPlatterStrongHyperdash().GetIssues(beatmap));

        using var spinner = CreateSpinner(gap: 50, nextX: 490);
        Assert.Empty(new CheckSpinnerTrap().GetIssues(spinner.GetBeatmap("Test")));
    }

    /// <summary> The distance that makes a hyperdash for a fruit after one at 900 ms. </summary>
    private static float GetTriggerDistance(int nextTime)
    {
        using var context = Create(Fruit(900, 16), Fruit(nextTime, 496), Fruit(5000, 496));
        AssertHyperdash(context);

        var objects = context.GetBeatmap("Test").GetCatchHitObjects(includeJuiceStreamParts: true);
        return objects[0].GetTriggerDistance(objects[1]);
    }

    private static void AssertHyperdash(CheckTestContext context) =>
        Assert.Equal(
            CatchMovementType.Hyperdash,
            context
                .GetBeatmap("Test")
                .GetCatchHitObjects(includeJuiceStreamParts: true)[0]
                .MovementType
        );

    private static List<BananaShowers.FallingBanana> GetBananas(CheckTestContext context) =>
        Assert.Single(BananaShowers.GetBananas(context.GetBeatmap("Test"))).Value;

    private static CheckTestContext CreateSpinner(int gap, int nextX) =>
        Create(
            Fruit(500, 256),
            TestHitObjects.Spinner(1000, 1300),
            Fruit(1300 + gap, nextX),
            Fruit(5000, nextX)
        );

    private static string Fruit(int time, int x) => TestHitObjects.Circle(time, x: x);

    private static List<Issue> Run<TCheck>(CheckTestContext context, Beatmap.Difficulty difficulty)
        where TCheck : BeatmapCheck, new()
    {
        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = difficulty;

        return new TCheck()
            .GetIssues(beatmap)
            .Where(issue => issue.AppliesToDifficulty(difficulty))
            .ToList();
    }

    private static CheckTestContext Create(params string[] hitObjects) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .CircleSize(4)
                .TimingPoints(TestTimingPoints.Uninherited(0, 60000d / 180))
                .HitObjects(hitObjects)
        );
}
