using MapsetVerifier.Checks.Taiko.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Compose;

public class CheckUnrankableFinishersTests
{
    /// <summary>
    ///     Notes at 166.67 bpm (360 ms per beat, so all snaps are whole ms), separated by the given divisor of a beat: d for don, k for kat, and upper case for
    ///     finishers.
    /// </summary>
    private static Issue.Level? Run(string notes, int divisor, Beatmap.Difficulty difficulty)
    {
        var hitObjects = notes.Select(
            (note, i) =>
                TestHitObjects.Circle(
                    (int)(1440 + i * 360d / divisor),
                    hitSound: (char.ToLower(note) == 'k' ? 8 : 0) + (char.IsUpper(note) ? 4 : 0)
                )
        );

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(TestTimingPoints.Uninherited(0, 360))
                .HitObjects(hitObjects)
        );

        return context
            .RunBeatmapCheck<CheckUnrankableFinishers>("Test")
            .SingleOrDefault(issue => issue.AppliesToDifficulty(difficulty))
            ?.level;
    }

    [Theory]
    [InlineData(1, Beatmap.Difficulty.Easy, null)]
    [InlineData(2, Beatmap.Difficulty.Easy, Issue.Level.Problem)]
    [InlineData(2, Beatmap.Difficulty.Normal, null)]
    [InlineData(3, Beatmap.Difficulty.Normal, Issue.Level.Problem)]
    [InlineData(3, Beatmap.Difficulty.Hard, Issue.Level.Warning)]
    [InlineData(4, Beatmap.Difficulty.Hard, Issue.Level.Problem)]
    [InlineData(6, Beatmap.Difficulty.Insane, Issue.Level.Problem)]
    public void FinisherEndingAPattern(
        int divisor,
        Beatmap.Difficulty difficulty,
        Issue.Level? expected
    )
    {
        Assert.Equal(expected, Run("ddK", divisor, difficulty));
    }

    // "Finisher notes for 1/4 patterns in this difficulty must only be used at the end of that pattern and be of
    // the opposite colour than its preceding note."
    [Theory]
    [InlineData("ddK", null)]
    [InlineData("ddD", Issue.Level.Problem)]
    [InlineData("dKd", Issue.Level.Problem)]
    public void Oni_QuarterPatterns(string notes, Issue.Level? expected)
    {
        Assert.Equal(expected, Run(notes, 4, Beatmap.Difficulty.Insane));
    }

    [Fact]
    public void RoundedSnaps_AreStillEvenlySpaced()
    {
        // 1/4 at 160 bpm is 93.75 ms, which alternates between 93 and 94 ms.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(TestTimingPoints.Uninherited(0, 375))
                .HitObjects(
                    TestHitObjects.Circle(1500),
                    TestHitObjects.Circle(1593, hitSound: 12),
                    TestHitObjects.Circle(1687)
                )
        );

        Assert.Contains(
            context.RunBeatmapCheck<CheckUnrankableFinishers>("Test"),
            issue =>
                issue.level == Issue.Level.Problem
                && issue.AppliesToDifficulty(Beatmap.Difficulty.Insane)
        );
    }

    [Theory]
    [InlineData("ddK", null)]
    [InlineData("ddD", Issue.Level.Warning)]
    public void InnerOni_IsOnlyWarned(string notes, Issue.Level? expected)
    {
        Assert.Equal(expected, Run(notes, 4, Beatmap.Difficulty.Expert));
        Assert.Equal(expected, Run(notes, 4, Beatmap.Difficulty.Ultra));
    }
}
