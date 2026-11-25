using MapsetVerifier.Checks.Taiko.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Compose;

public class CheckPatternLengthTests
{
    /// <summary> Notes in a row at 160 bpm, separated by the given divisor of a beat, and one after a rest. </summary>
    private static List<Issue> Run(int notes, int divisor)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .Version("Oni")
                .TimingPoints(TestTimingPoints.Uninherited(0, 375))
                .HitObjects(
                    Enumerable
                        .Range(0, notes)
                        .Select(i => TestHitObjects.Circle((int)(3000 + i * 375d / divisor)))
                )
                .HitObjects(TestHitObjects.Circle(60000))
        );

        return context.RunBeatmapSetCheck<CheckPatternLength>();
    }

    // As long as the guideline allows is mentioned, and longer is warned about.
    [Theory]
    [InlineData(Beatmap.Difficulty.Easy, 2, 2)]
    [InlineData(Beatmap.Difficulty.Normal, 2, 5)] // 7 in sets with a Kantan.
    [InlineData(Beatmap.Difficulty.Normal, 3, 2)]
    [InlineData(Beatmap.Difficulty.Hard, 4, 5)]
    [InlineData(Beatmap.Difficulty.Hard, 6, 4)]
    [InlineData(Beatmap.Difficulty.Insane, 4, 9)]
    public void LengthOfAPattern(Beatmap.Difficulty difficulty, int divisor, int limit)
    {
        Issue.Level? Of(int notes) =>
            Run(notes, divisor)
                .SingleOrDefault(issue =>
                    issue.AppliesToDifficulty(difficulty)
                    && issue.message.Contains($"1/{divisor} pattern")
                )
                ?.level;

        Assert.Null(Of(limit - 1));
        Assert.Equal(Issue.Level.Minor, Of(limit));
        Assert.Equal(Issue.Level.Warning, Of(limit + 1));
    }

    [Fact]
    public void LongPattern_IsAWarning()
    {
        var issue = Assert.Single(
            Run(40, 4),
            issue => issue.AppliesToDifficulty(Beatmap.Difficulty.Insane)
        );
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("1/4 pattern is 40 notes long", issue.message);
    }
}
