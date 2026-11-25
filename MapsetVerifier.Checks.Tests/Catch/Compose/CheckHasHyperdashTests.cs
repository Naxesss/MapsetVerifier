using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CheckHasHyperdashTests
{
    private static List<Issue> Run(CatchMovementType movement)
    {
        using var context = CheckTestContext.CreateFromOsu(CatchObjects.Beatmap(1000, 1200));
        CatchObjects.Set(context, movement);

        return context.RunBeatmapCheck<CheckHasHyperdash>("Test");
    }

    [Fact]
    public void Hyperdash_IsAProblemInCupsAndSalads()
    {
        var issue = Assert.Single(Run(CatchMovementType.Hyperdash));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Easy));
        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Normal));
        Assert.False(issue.AppliesToDifficulty(Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void Dash_IsFine()
    {
        Assert.Empty(Run(CatchMovementType.Dash));
    }

    [Theory]
    [InlineData("head", 1, 0)]
    [InlineData("repeat", 1, 1)]
    [InlineData("tail", 1, 0)]
    public void Platters_MayNotHaveThemOnRepeats(string part, int salad, int platter)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, "L", "400:192", 2, 140, 100),
                    TestHitObjects.Circle(3000)
                )
        );
        CatchObjects.Set(context);
        var slider = context.GetBeatmap("Test").HitObjects.OfType<JuiceStream>().Single();

        foreach (var other in slider.Parts)
            other.MovementType = CatchMovementType.Walk;

        if (part == "head")
            slider.MovementType = CatchMovementType.Hyperdash;
        else if (part == "repeat")
            slider
                .Parts.First(p => p.Kind == JuiceStream.JuiceStreamPart.PartKind.Repeat)
                .MovementType = CatchMovementType.Hyperdash;
        else
            slider.Parts[^1].MovementType = CatchMovementType.Hyperdash;

        var issues = context.RunBeatmapCheck<CheckHasHyperdash>("Test");

        Assert.Equal(
            salad,
            issues.Count(issue => issue.AppliesToDifficulty(Beatmap.Difficulty.Normal))
        );
        Assert.Equal(
            platter,
            issues.Count(issue => issue.AppliesToDifficulty(Beatmap.Difficulty.Hard))
        );
    }
}
