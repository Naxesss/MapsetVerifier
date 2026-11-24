using MapsetVerifier.Checks.Taiko.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Settings;

public class CheckDiffSettingsTaikoTests
{
    [Fact]
    public void Easy_MatchingRecommendedValues_ProducesNoIssues()
    {
        // No hit objects => 0 drain time => drain index 0 => recommended HP 9 for Easy.
        using var context = CreateContext(od: 3, hp: 9);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettingsTaiko().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Easy_HpSlightlyOff_ProducesMinorIssue()
    {
        using var context = CreateContext(od: 3, hp: 8.5f);

        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issues = new CheckDiffSettingsTaiko().GetIssues(beatmap).ToList();

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

        var issues = new CheckDiffSettingsTaiko().GetIssues(beatmap).ToList();

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

        var issues = new CheckDiffSettingsTaiko().GetIssues(beatmap).ToList();

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

        var issues = new CheckDiffSettingsTaiko().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    private static CheckTestContext CreateContext(float od, float hp) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .Title("Test")
                .Artist("Tests")
                .Creator("Tests")
                .HP(hp)
                .OD(od),
            extraFiles: ["audio.mp3"]
        );

    [Theory]
    [InlineData(60, 9)]
    [InlineData(61, 8)]
    [InlineData(224, 8)]
    [InlineData(225, 7)]
    [InlineData(284, 7)]
    [InlineData(285, 6)]
    public void RecommendedHp_FollowsTheDrainTime(int seconds, float hp)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .HP(hp)
                .OD(3.5f)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Circle(1000),
                    TestHitObjects.Circle(1000 + seconds * 1000)
                )
        );
        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Easy;

        var issue = Assert.Single(new CheckDiffSettingsTaiko().GetIssues(beatmap));

        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("OD is different from suggested value 3", issue.message);
    }
}
