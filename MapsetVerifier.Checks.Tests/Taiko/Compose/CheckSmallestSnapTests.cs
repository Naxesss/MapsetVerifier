using MapsetVerifier.Checks.Taiko.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Compose;

public class CheckSmallestSnapTests
{
    // 160 bpm, which is used as is.
    private static List<Issue> Run(params int[] times)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(TestTimingPoints.Uninherited(0, 375))
                .HitObjects(times.Select(time => TestHitObjects.Circle(time)))
        );

        return context.RunBeatmapCheck<CheckSmallestSnap>("Test");
    }

    private static Beatmap.Difficulty[] Applying(List<Issue> issues) =>
        Enum.GetValues<Beatmap.Difficulty>()
            .Where(difficulty => issues.Any(issue => issue.AppliesToDifficulty(difficulty)))
            .ToArray();

    [Theory]
    [InlineData(188, new Beatmap.Difficulty[0])] // 1/2
    [InlineData(125, new[] { Beatmap.Difficulty.Easy })] // 1/3
    [InlineData(94, new[] { Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal })] // 1/4
    [InlineData(
        47, // 1/8
        new[]
        {
            Beatmap.Difficulty.Easy,
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
        }
    )]
    public void SmallerSnapThanTheDifficultyAllows(int gap, Beatmap.Difficulty[] expected)
    {
        Assert.Equal(expected, Applying(Run(1000, 1000 + gap, 5000, 8000)));
    }

    [Fact]
    public void Group_IsReportedOnce()
    {
        var issue = Assert.Single(
            Run(1000, 1094, 1188, 5000),
            issue => issue.AppliesToDifficulty(Beatmap.Difficulty.Normal)
        );

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("(1,2,3)", issue.message);
    }

    [Fact]
    public void Group_AtTheEnd_IsReportedToo()
    {
        Assert.NotEmpty(Run(1000, 5000, 5094, 5188));
    }
}
