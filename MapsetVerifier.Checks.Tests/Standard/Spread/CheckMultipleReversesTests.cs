using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckMultipleReversesTests
{
    // One beat of 500 ms is 140 px.
    private static List<Issue> Run(double length, int slides = 3)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .WithDefaultTiming()
                .HitObjects(TestHitObjects.Slider(1000, slides: slides, length: length))
        );

        return context.RunBeatmapCheck<CheckMultipleReverses>("Test");
    }

    // Easy: 1/1 at 240 bpm (250 ms) or 180 bpm (333 ms) in total. Normal: half of that.
    [Theory]
    [InlineData(42, null, null)] // 450 ms
    [InlineData(28, Issue.Level.Warning, null)] // 300 ms
    [InlineData(21, Issue.Level.Problem, null)] // 225 ms
    [InlineData(14, Issue.Level.Problem, Issue.Level.Warning)] // 150 ms
    [InlineData(7, Issue.Level.Problem, Issue.Level.Problem)] // 75 ms
    public void ShortSliders(double length, Issue.Level? easy, Issue.Level? normal)
    {
        var issues = Run(length);

        Assert.Equal(
            easy,
            issues
                .SingleOrDefault(issue => issue.AppliesToDifficulty(Beatmap.Difficulty.Easy))
                ?.level
        );
        Assert.Equal(
            normal,
            issues
                .SingleOrDefault(issue => issue.AppliesToDifficulty(Beatmap.Difficulty.Normal))
                ?.level
        );
    }

    [Fact]
    public void SingleReverse_IsFine()
    {
        Assert.Empty(Run(7, 2));
    }
}
