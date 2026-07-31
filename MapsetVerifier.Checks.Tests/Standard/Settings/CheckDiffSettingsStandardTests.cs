using MapsetVerifier.Checks.Standard.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Settings;

public class CheckDiffSettingsStandardTests
{
    [Fact]
    public void Easy_WithinGuidelineRange_ProducesNoIssues()
    {
        using var context = CreateContext(ar: 5, od: 2, hp: 2, cs: 4);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettingsStandard().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Easy_ApproachRateTooHigh_ProducesMinorIssue()
    {
        using var context = CreateContext(ar: 6, od: 2, hp: 2, cs: 4);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettingsStandard().GetIssues(beatmap).ToList();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("AR", issue.message);
        Assert.Contains("5 or lower", issue.message);
    }

    [Fact]
    public void Expert_MultipleSettingsOutsideRange_ProducesMultipleIssues()
    {
        using var context = CreateContext(ar: 7, od: 7, hp: 4, cs: 8);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Expert;

        var issues = new CheckDiffSettingsStandard().GetIssues(beatmap).ToList();

        Assert.Equal(4, issues.Count);
        Assert.Contains(issues, i => i.message.Contains("AR") && i.message.Contains("8 or higher"));
        Assert.Contains(issues, i => i.message.Contains("OD") && i.message.Contains("8 or higher"));
        Assert.Contains(issues, i => i.message.Contains("HP") && i.message.Contains("5 or higher"));
        Assert.Contains(issues, i => i.message.Contains("CS") && i.message.Contains("7 or lower"));
    }

    [Fact]
    public void Ultra_HasNoGuideline_ProducesNoIssues()
    {
        using var context = CreateContext(ar: 10, od: 10, hp: 10, cs: 10);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Ultra;

        var issues = new CheckDiffSettingsStandard().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    private static CheckTestContext CreateContext(float ar, float od, float hp, float cs) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Standard)
                .Title("Test")
                .Artist("Tests")
                .Creator("Tests")
                .CircleSize(cs)
                .HP(hp)
                .OD(od)
                .AR(ar),
            extraFiles: ["audio.mp3"]
        );
}
