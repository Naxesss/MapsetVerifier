using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CheckSpinnerGapTests
{
    private static List<Issue> Run(int before, int after)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Circle(1000),
                    TestHitObjects.Spinner(1000 + before, 5000),
                    TestHitObjects.Circle(5000 + after)
                )
        );

        return context.RunBeatmapCheck<CheckSpinnerGap>("Test");
    }

    private static bool Any(List<Issue> issues, Beatmap.Difficulty difficulty) =>
        issues.Any(issue => issue.AppliesToDifficulty(difficulty));

    // From the ranking criteria of each difficulty.
    [Theory]
    [InlineData(Beatmap.Difficulty.Easy, 250, 250)]
    [InlineData(Beatmap.Difficulty.Normal, 250, 250)]
    [InlineData(Beatmap.Difficulty.Hard, 125, 250)]
    [InlineData(Beatmap.Difficulty.Insane, 125, 125)]
    [InlineData(Beatmap.Difficulty.Expert, 62, 125)]
    public void GapAroundASpinner(Beatmap.Difficulty difficulty, int before, int after)
    {
        Assert.False(Any(Run(before, after), difficulty));
        Assert.True(Any(Run(before - 1, after), difficulty));
        Assert.True(Any(Run(before, after - 1), difficulty));
    }

    [Fact]
    public void Ultra_HasTheLimitsOfAnOverdose()
    {
        Assert.True(Any(Run(61, 1000), Beatmap.Difficulty.Ultra));
        Assert.True(Any(Run(1000, 124), Beatmap.Difficulty.Ultra));
    }
}
