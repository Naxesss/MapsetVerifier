using MapsetVerifier.Checks.AllModes.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Settings;

public class CheckDiffSettingsAllModesTests
{
    private static List<Issue> Run(OsuBuilder builder)
    {
        using var context = CheckTestContext.CreateFromOsu(
            builder.WithDefaultTiming().WithDefaultHitObject()
        );

        return context.RunBeatmapCheck<CheckDiffSettingsAllModes>("Test");
    }

    public static TheoryData<float> OneDecimal() =>
        [.. Enumerable.Range(0, 101).Select(i => i / 10f)];

    [Theory]
    [MemberData(nameof(OneDecimal))]
    public void AnyValueWithOneDecimal_IsFine(float value)
    {
        Assert.Empty(Run(new OsuBuilder().HP(value).CircleSize(value).AR(value).OD(value)));
    }

    [Theory]
    [InlineData(4.25f)]
    [InlineData(9.99f)]
    [InlineData(0.01f)]
    public void MoreDecimals_AreAProblem_InEachSetting(float value)
    {
        var issues = Run(new OsuBuilder().HP(value).CircleSize(value).AR(value).OD(value));

        Assert.Equal(4, issues.Count);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(18, false)]
    public void ManiaKeyCount(float keys, bool isProblem)
    {
        var issues = Run(new OsuBuilder().Mode(Beatmap.Mode.Mania).CircleSize(keys));

        Assert.Equal(isProblem, issues.Count == 1);
    }

    [Fact]
    public void AboveTen_IsWarnedAbout()
    {
        var issue = Assert.Single(Run(new OsuBuilder().HP(11)));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("11", issue.message);
    }
}
