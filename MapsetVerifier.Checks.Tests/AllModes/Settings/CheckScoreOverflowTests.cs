using MapsetVerifier.Checks.AllModes.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Settings;

public class CheckScoreOverflowTests
{
    private static List<Issue> Run(Beatmap.Mode mode, int objects)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(mode)
                .WithDefaultTiming()
                .HitObjects(
                    Enumerable.Range(0, objects).Select(i => TestHitObjects.Circle(1000 + i * 250))
                )
        );

        return context.RunBeatmapCheck<CheckScoreOverflow>("Test");
    }

    [Fact]
    public void ShortBeatmap_IsFine()
    {
        Assert.Empty(Run(Beatmap.Mode.Taiko, 500));
        Assert.Empty(Run(Beatmap.Mode.Standard, 0));
    }

    [Fact]
    public void LongCatchBeatmap_OverflowsItsScore()
    {
        // Tens of thousands of objects, each worth at least 300 times its combo's multiplier.
        var issue = Assert.Single(
            Run(Beatmap.Mode.Catch, 20000),
            issue => issue.message.Contains("Score overflows")
        );

        Assert.Equal(Issue.Level.Problem, issue.level);
    }

    [Fact]
    public void Mania_CapsItsScore()
    {
        Assert.Empty(Run(Beatmap.Mode.Mania, 20000));
    }

    /// <summary> 2 px per slide, each of which is worth one combo, as is the head. </summary>
    [Theory]
    [InlineData(8190, false)] // 7 * 8192 + 8191 = 65535 in total.
    [InlineData(8191, true)]
    public void Combo_HasALimitInStable(int lastSlides, bool expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .WithDefaultTiming()
                .HitObjects(
                    Enumerable
                        .Range(0, 8)
                        .Select(i =>
                            TestHitObjects.Slider(
                                1000 + i * 1000000,
                                "L",
                                "258:192",
                                i == 7 ? lastSlides : 8191,
                                2
                            )
                        )
                )
        );

        Assert.Equal(
            expected,
            context
                .RunBeatmapCheck<CheckScoreOverflow>("Test")
                .Any(issue => issue.message.Contains("65,536"))
        );
    }
}
