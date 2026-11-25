using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckSpinnerRecoveryTests
{
    private static List<Issue> Run(int length, int recovery, double msPerBeat = 250)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .TimingPoints(TestTimingPoints.Uninherited(0, msPerBeat))
                .HitObjects(
                    TestHitObjects.Spinner(1000, 1000 + length),
                    TestHitObjects.Circle(1000 + length + recovery)
                )
        );

        return context.RunBeatmapCheck<CheckSpinnerRecovery>("Test");
    }

    private static Issue.Level? Level(
        List<Issue> issues,
        Beatmap.Difficulty difficulty,
        string text
    ) =>
        issues
            .SingleOrDefault(issue =>
                issue.AppliesToDifficulty(difficulty) && issue.message.Contains(text)
            )
            ?.level;

    // 4, 3 and 2 beats at 240 bpm.
    [Theory]
    [InlineData(Beatmap.Difficulty.Easy, 1000)]
    [InlineData(Beatmap.Difficulty.Normal, 750)]
    [InlineData(Beatmap.Difficulty.Hard, 500)]
    public void Length(Beatmap.Difficulty difficulty, int threshold)
    {
        Issue.Level? Of(int length) => Level(Run(length, 5000), difficulty, "length");

        Assert.Equal(Issue.Level.Problem, Of(threshold - 1));
        Assert.Equal(Issue.Level.Warning, Of(threshold));
        Assert.Equal(Issue.Level.Warning, Of((int)(threshold * 1.2) - 1));
        Assert.Null(Of((int)(threshold * 1.2)));
    }

    // 4, 2 and 1 beats at 240 bpm.
    [Theory]
    [InlineData(Beatmap.Difficulty.Easy, 1000)]
    [InlineData(Beatmap.Difficulty.Normal, 500)]
    [InlineData(Beatmap.Difficulty.Hard, 250)]
    public void Recovery(Beatmap.Difficulty difficulty, int threshold)
    {
        Issue.Level? Of(int recovery) => Level(Run(5000, recovery), difficulty, "recovery");

        Assert.Equal(Issue.Level.Problem, Of(threshold - 1));
        Assert.Equal(Issue.Level.Warning, Of(threshold));
        Assert.Null(Of((int)(threshold * 1.2)));
    }

    [Fact]
    public void Recovery_IsShorter_AtALowerBpm()
    {
        // 120 bpm scales to half the speed, so half the recovery is enough.
        Assert.Null(Level(Run(5000, 600, 500), Beatmap.Difficulty.Easy, "recovery"));
        Assert.Equal(
            Issue.Level.Problem,
            Level(Run(5000, 499, 500), Beatmap.Difficulty.Easy, "recovery")
        );
    }

    [Fact]
    public void HarderDifficulties_AreLeftOut()
    {
        Assert.Null(Level(Run(100, 10), Beatmap.Difficulty.Insane, "Spinner"));
    }

    [Fact]
    public void BetweenSpinners_NoRecoveryIsNeeded()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .WithDefaultTiming()
                .HitObjects(TestHitObjects.Spinner(1000, 5000), TestHitObjects.Spinner(5010, 9000))
        );

        Assert.Empty(context.RunBeatmapCheck<CheckSpinnerRecovery>("Test"));
    }
}
