using MapsetVerifier.Checks.Mania.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Settings;

public class CheckDiffSettingsLowerDifficultiesTests
{
    [Fact]
    public void Easy_WithinLimits_ProducesNoIssues()
    {
        using var context = CreateContext(od: 7, hp: 7);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettingsLowerDifficulties().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Easy_HpOverLimit_ProducesWarningIssue()
    {
        using var context = CreateContext(od: 7, hp: 8);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettingsLowerDifficulties().GetIssues(beatmap).ToList();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("should not have a HP value over 7", issue.message);
    }

    [Fact]
    public void Normal_OdOverLimit_ProducesWarningIssue()
    {
        using var context = CreateContext(od: 8, hp: 7);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Normal;

        var issues = new CheckDiffSettingsLowerDifficulties().GetIssues(beatmap).ToList();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("should not have an OD value over 7", issue.message);
    }

    [Fact]
    public void Hard_WithinLimits_ProducesNoIssues()
    {
        using var context = CreateContext(od: 8, hp: 8);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Hard;

        var issues = new CheckDiffSettingsLowerDifficulties().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Ultra_ProducesAmbiguousIssue()
    {
        using var context = CreateContext(od: 5, hp: 5);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Ultra;

        var issues = new CheckDiffSettingsLowerDifficulties().GetIssues(beatmap).ToList();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("is ambiguous", issue.message);
    }

    [Fact]
    public void Insane_HasNoGuideline_ProducesNoIssues()
    {
        using var context = CreateContext(od: 10, hp: 10);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Insane;

        var issues = new CheckDiffSettingsLowerDifficulties().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    private static CheckTestContext CreateContext(float od, float hp) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .Title("Test")
                .Artist("Tests")
                .Creator("Tests")
                .HP(hp)
                .OD(od),
            extraFiles: ["audio.mp3"]
        );
}
