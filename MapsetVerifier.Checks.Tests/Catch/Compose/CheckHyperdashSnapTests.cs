using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CheckHyperdashSnapTests
{
    private static List<Issue> Run(int ms, CatchMovementType movement)
    {
        using var context = CheckTestContext.CreateFromOsu(CatchObjects.Beatmap(1000, 1000 + ms));
        CatchObjects.Set(context, movement);

        return context.RunBeatmapCheck<CheckHyperdashSnap>("Test");
    }

    [Theory]
    [InlineData(125, false, false)]
    [InlineData(124, true, false)]
    [InlineData(62, true, false)]
    [InlineData(61, true, true)]
    public void Hyperdash_FasterThanTheDifficultyAllows(int ms, bool platter, bool rain)
    {
        var issues = Run(ms, CatchMovementType.Hyperdash);

        Assert.Equal(
            platter,
            issues.Any(issue => issue.AppliesToDifficulty(Beatmap.Difficulty.Hard))
        );
        Assert.Equal(
            rain,
            issues.Any(issue => issue.AppliesToDifficulty(Beatmap.Difficulty.Insane))
        );
    }

    [Fact]
    public void Dash_IsFine()
    {
        Assert.Empty(Run(50, CatchMovementType.Dash));
    }
}
