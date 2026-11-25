using MapsetVerifier.Checks.Catch.Compose.Rain;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose.Rain;

public class CheckRainConsistentHyperdashSnapsTests
{
    private static List<Issue> Run(params int[] times)
    {
        using var context = CheckTestContext.CreateFromOsu(CatchObjects.Beatmap(times));
        CatchObjects.Set(
            context,
            Enumerable.Repeat(CatchMovementType.Hyperdash, times.Length - 1).ToArray()
        );

        return context.RunBeatmapCheck<CheckRainConsistentHyperdashSnaps>("Test");
    }

    [Fact]
    public void SameSnap_IsFine()
    {
        Assert.Empty(Run(1000, 1200, 1400, 1600));
    }

    [Fact]
    public void AnotherSnap_IsAWarning()
    {
        var issue = Assert.Single(Run(1000, 1200, 1600));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Insane));
    }

    [Fact]
    public void HigherSnapped_IsLeftToAnotherCheck()
    {
        Assert.Empty(Run(1000, 1100, 1400));
    }
}
