using MapsetVerifier.Checks.Taiko.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Compose;

public class CheckRestMomentsTests
{
    /// <summary> A note on each beat at 160 bpm, apart from the beats left out. </summary>
    private static List<Issue> Run(int beats, params int[] without)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(TestTimingPoints.Uninherited(0, 375))
                .HitObjects(
                    Enumerable
                        .Range(0, beats + 1)
                        .Except(without)
                        .Select(i => TestHitObjects.Circle(3000 + i * 375))
                )
        );

        return context.RunBeatmapCheck<CheckRestMoments>("Test");
    }

    private static Issue.Level? Level(List<Issue> issues, Beatmap.Difficulty difficulty) =>
        issues.SingleOrDefault(issue => issue.AppliesToDifficulty(difficulty))?.level;

    [Theory]
    [InlineData(36, null)]
    [InlineData(37, Issue.Level.Minor)]
    [InlineData(44, Issue.Level.Minor)]
    [InlineData(45, Issue.Level.Warning)]
    public void ContinuousMapping(int beats, Issue.Level? expected)
    {
        var issues = Run(beats);

        Assert.Equal(expected, Level(issues, Beatmap.Difficulty.Easy));
        Assert.Equal(expected, Level(issues, Beatmap.Difficulty.Normal));
        // 1/1 gaps are rest moments enough for an Oni, and three of them for a Muzukashii.
        Assert.Null(Level(issues, Beatmap.Difficulty.Hard));
        Assert.Null(Level(issues, Beatmap.Difficulty.Insane));
    }

    [Fact]
    public void Rest_SplitsItUp()
    {
        // A 2/1 gap is enough for a Futsuu, but a Kantan needs 3/1.
        var two = Run(50, 25);
        Assert.Null(Level(two, Beatmap.Difficulty.Normal));
        Assert.Equal(Issue.Level.Warning, Level(two, Beatmap.Difficulty.Easy));

        Assert.Null(Level(Run(50, 25, 26), Beatmap.Difficulty.Easy));
    }
}
