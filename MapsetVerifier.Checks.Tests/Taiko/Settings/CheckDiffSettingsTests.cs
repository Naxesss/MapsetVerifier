using MapsetVerifier.Checks.Taiko.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Settings;

public class CheckDiffSettingsTests
{
    [Fact]
    public void Easy_MatchingRecommendedValues_ProducesNoIssues()
    {
        // No hit objects => 0 drain time => drain index 0 => recommended HP 9 for Easy.
        using var context = CreateContext(od: 3, hp: 9);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Easy_HpSlightlyOff_ProducesMinorIssue()
    {
        using var context = CreateContext(od: 3, hp: 8.5f);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("HP is different from suggested value 9", issue.message);
    }

    [Fact]
    public void Easy_HpFarOff_ProducesWarningIssue()
    {
        using var context = CreateContext(od: 3, hp: 6);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("HP is different from suggested value 9", issue.message);
    }

    [Fact]
    public void Easy_OdFarOff_ProducesWarningIssue()
    {
        using var context = CreateContext(od: 5, hp: 9);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("OD is different from suggested value 3", issue.message);
    }

    [Fact]
    public void Expert_HasNoGuideline_ProducesNoIssues()
    {
        using var context = CreateContext(od: 10, hp: 10);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Expert;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    private static CheckTestContext CreateContext(float od, float hp) =>
        CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    string.Join(
                        "\n",
                        "osu file format v14",
                        "[General]",
                        "AudioFilename:audio.mp3",
                        $"Mode: {(int)Beatmap.Mode.Taiko}",
                        "[Metadata]",
                        "Title:Test",
                        "Artist:Tests",
                        "Creator:Tests",
                        "Version:Test",
                        "[Difficulty]",
                        "CircleSize:4",
                        $"HPDrainRate:{hp}",
                        $"OverallDifficulty:{od}",
                        "ApproachRate:5",
                        "SliderMultiplier:1.4",
                        "SliderTickRate:1",
                        "[Events]",
                        "[TimingPoints]",
                        "[HitObjects]"
                    )
                ),
            ],
            extraFiles: ["audio.mp3"]
        );
}
