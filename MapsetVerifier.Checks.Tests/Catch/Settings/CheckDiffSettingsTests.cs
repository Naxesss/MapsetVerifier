using MapsetVerifier.Checks.Catch.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Settings;

public class CheckDiffSettingsTests
{
    [Fact]
    public void Insane_WithinGuidelineRange_ProducesNoIssues()
    {
        using var context = CreateContext(ar: 9, od: 9, hp: 5, cs: 4);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Insane;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Insane_ApproachRateTooHigh_ProducesMinorIssue()
    {
        using var context = CreateContext(ar: 9.5f, od: 9, hp: 5, cs: 4);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Insane;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("AR", issue.message);
        Assert.Contains("9 or lower", issue.message);
    }

    [Fact]
    public void Expert_MultipleSettingsOutsideRange_ProducesMultipleIssues()
    {
        using var context = CreateContext(ar: 8, od: 8, hp: 4, cs: 3);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Expert;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        Assert.Equal(4, issues.Count);
        Assert.Contains(issues, i => i.message.Contains("AR") && i.message.Contains("9 or higher"));
        Assert.Contains(issues, i => i.message.Contains("OD") && i.message.Contains("9 or higher"));
        Assert.Contains(issues, i => i.message.Contains("HP") && i.message.Contains("5 or higher"));
        Assert.Contains(issues, i => i.message.Contains("CS") && i.message.Contains("4 or higher"));
    }

    [Fact]
    public void Ultra_HasNoGuideline_ProducesNoIssues()
    {
        using var context = CreateContext(ar: 10, od: 10, hp: 10, cs: 10);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Ultra;

        var issues = new CheckDiffSettings().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    private static CheckTestContext CreateContext(float ar, float od, float hp, float cs) =>
        CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    string.Join(
                        "\n",
                        "osu file format v14",
                        "[General]",
                        "AudioFilename:audio.mp3",
                        $"Mode: {(int)Beatmap.Mode.Catch}",
                        "[Metadata]",
                        "Title:Test",
                        "Artist:Tests",
                        "Creator:Tests",
                        "Version:Test",
                        "[Difficulty]",
                        $"CircleSize:{cs}",
                        $"HPDrainRate:{hp}",
                        $"OverallDifficulty:{od}",
                        $"ApproachRate:{ar}",
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
