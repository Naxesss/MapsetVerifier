using MapsetVerifier.Checks.Taiko.Compose;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Compose;

public class CheckSpinnerReadabilityTests
{
    // 160 bpm, so 1/2 is 187.5 ms and 1/4 is 93.75 ms.
    [Theory]
    [InlineData(188, new Beatmap.Difficulty[0])]
    [InlineData(
        187,
        new[] { Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal, Beatmap.Difficulty.Hard }
    )]
    [InlineData(
        94,
        new[] { Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal, Beatmap.Difficulty.Hard }
    )]
    [InlineData(
        93,
        new[]
        {
            Beatmap.Difficulty.Easy,
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
            Beatmap.Difficulty.Expert,
        }
    )]
    public void GapBeforeASpinner(int gap, Beatmap.Difficulty[] expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(TestTimingPoints.Uninherited(0, 375))
                .HitObjects(
                    TestHitObjects.Circle(1000),
                    TestHitObjects.Spinner(1000 + gap, 3000),
                    TestHitObjects.Circle(3001)
                )
        );

        var issues = context.RunBeatmapCheck<CheckSpinnerReadability>("Test");

        Assert.Equal(
            expected,
            Enum.GetValues<Beatmap.Difficulty>()
                .Where(difficulty => issues.Any(issue => issue.AppliesToDifficulty(difficulty)))
        );
    }
}
