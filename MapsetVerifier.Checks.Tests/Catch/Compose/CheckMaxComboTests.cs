using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CheckMaxComboTests
{
    // Circle with new combo (1 | 4) and slider with new combo (2 | 4).
    private const int NewComboCircle = 5;
    private const int NewComboSlider = 6;

    [Fact]
    public void LongSlider_DropletsDoNotCountTowardsCombo()
    {
        // 2800px at 1.4 SV and 500ms beats lasts 20 beats, so the slider has 19 droplets but only
        // its head and tail stack on the plate.
        using var context = CreateContext(
            $"256,192,0,{NewComboCircle},0,0:0:0:0:",
            TestHitObjects.Slider(1000, curvePoints: "256:3000", length: 2800, type: NewComboSlider)
        );

        var issues = RunForDifficulty(context, Beatmap.Difficulty.Easy);

        Assert.Empty(issues);
    }

    [Fact]
    public void FirstCombo_CountsTheFirstObject()
    {
        // Nine fruits in the first combo exceed the Cup guideline of 8.
        using var context = CreateContext(
            Enumerable.Range(0, 9).Select(i => TestHitObjects.Circle(i * 500)).ToArray()
        );

        var issues = RunForDifficulty(context, Beatmap.Difficulty.Easy);

        var issue = Assert.Single(issues);
        Assert.Contains("currently it has 9", issue.message);
    }

    private static List<Framework.Objects.Issue> RunForDifficulty(
        CheckTestContext context,
        Beatmap.Difficulty difficulty
    )
    {
        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = difficulty;

        return new CheckMaxCombo()
            .GetIssues(beatmap)
            .Where(issue => issue.AppliesToDifficulty(difficulty))
            .ToList();
    }

    private static CheckTestContext CreateContext(params string[] hitObjects) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder().Mode(Beatmap.Mode.Catch).WithDefaultTiming().HitObjects(hitObjects),
            extraFiles: ["audio.mp3"]
        );
}
