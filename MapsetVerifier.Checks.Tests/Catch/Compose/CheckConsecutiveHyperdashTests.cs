using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CheckConsecutiveHyperdashTests
{
    /// <summary> Objects separated by the given ms, all of them hyperdashes up to the last two. </summary>
    private static List<Issue> Run(params int[] gaps)
    {
        var times = new List<int> { 1000 };
        foreach (var gap in gaps)
            times.Add(times[^1] + gap);
        times.Add(times[^1] + 1000);

        using var context = CheckTestContext.CreateFromOsu(CatchObjects.Beatmap([.. times]));
        CatchObjects.Set(
            context,
            Enumerable.Repeat(CatchMovementType.Hyperdash, gaps.Length).ToArray()
        );

        return context.RunBeatmapCheck<CheckConsecutiveHyperdash>("Test");
    }

    private static string? Message(List<Issue> issues, Beatmap.Difficulty difficulty) =>
        issues.SingleOrDefault(issue => issue.AppliesToDifficulty(difficulty))?.message;

    [Fact]
    public void Platter_AllowsTwoBasicSnapped()
    {
        Assert.Null(Message(Run(300, 300), Beatmap.Difficulty.Hard));
        Assert.Contains(
            "basic-snapped hyperdashes were used and should be at most 2, currently 3",
            Message(Run(300, 300, 300), Beatmap.Difficulty.Hard)
        );
    }

    [Fact]
    public void Platter_AllowsOneHigherSnapped()
    {
        Assert.Null(Message(Run(200), Beatmap.Difficulty.Hard));
        Assert.Contains(
            "higher-snapped hyperdashes were used and should be at most 1, currently 2",
            Message(Run(200, 300), Beatmap.Difficulty.Hard)
        );
    }

    [Fact]
    public void Platter_LastHyperdashOfTheChain_CountsAsHigherSnappedToo()
    {
        Assert.Contains(
            "higher-snapped",
            Message(Run(300, 200), Beatmap.Difficulty.Hard) ?? "none"
        );
    }

    [Fact]
    public void Rain_AllowsFourBasicSnapped()
    {
        Assert.Null(Message(Run(150, 150, 150, 150), Beatmap.Difficulty.Insane));
        Assert.Contains(
            "at most 4, currently 5",
            Message(Run(150, 150, 150, 150, 150), Beatmap.Difficulty.Insane)
        );
    }

    [Fact]
    public void Rain_AllowsOneHigherSnapped()
    {
        Assert.Contains(
            "higher-snapped hyperdashes were used and should be at most 1, currently 2",
            Message(Run(100, 150), Beatmap.Difficulty.Insane)
        );
    }

    [Fact]
    public void ChainUpToTheEndOfTheMap_IsFoundToo()
    {
        using var context = CheckTestContext.CreateFromOsu(
            CatchObjects.Beatmap(1000, 1300, 1600, 1900)
        );
        CatchObjects.Set(context, Enumerable.Repeat(CatchMovementType.Hyperdash, 4).ToArray());

        Assert.Contains(
            "at most 2, currently",
            Message(
                context.RunBeatmapCheck<CheckConsecutiveHyperdash>("Test"),
                Beatmap.Difficulty.Hard
            ) ?? "none"
        );
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void Rain_AllowsFewerWithinSliders(int count, bool expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, "L", "380:100", 4, 100, 0, 100),
                    TestHitObjects.Circle(5000, 0)
                )
        );
        CatchObjects.Set(context);
        var slider = (JuiceStream)context.GetBeatmap("Test").HitObjects[0];
        var objects = slider.Parts.Cast<ICatchHitObject>().Prepend(slider).ToList();
        for (var i = 0; i < objects.Count; i++)
            objects[i].MovementType =
                i < count ? CatchMovementType.Hyperdash : CatchMovementType.Walk;

        Assert.Equal(
            expected,
            Message(
                context.RunBeatmapCheck<CheckConsecutiveHyperdash>("Test"),
                Beatmap.Difficulty.Insane
            )
                ?.Contains("within a slider")
                ?? false
        );
    }
}
