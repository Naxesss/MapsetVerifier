using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckInconsistentLinesTests
{
    private static List<Issue> Run(string[] first, string[] second)
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", Build("A", first)),
            ("b.osu", Build("B", second)),
        ]);

        return context.RunBeatmapSetCheck<CheckInconsistentLines>();
    }

    private static string Build(string version, string[] timingPoints) =>
        new OsuBuilder().Version(version).TimingPoints(timingPoints).WithDefaultHitObject().Build();

    [Fact]
    public void SameRedLines_AreFine_WhateverTheGreenOnes()
    {
        Assert.Empty(
            Run(
                [TestTimingPoints.Uninherited(0, 500)],
                [TestTimingPoints.Uninherited(0, 500), TestTimingPoints.Inherited(1000, -50)]
            )
        );
    }

    [Fact]
    public void MissingLine_IsAProblem_InWhicheverDifficultyLacksIt()
    {
        string[] one = [TestTimingPoints.Uninherited(0, 500)];
        string[] two =
        [
            TestTimingPoints.Uninherited(0, 500),
            TestTimingPoints.Uninherited(5000, 400),
        ];

        var missingInSecond = Assert.Single(Run(two, one));
        Assert.Equal(Issue.Level.Problem, missingInSecond.level);
        Assert.Equal("B", missingInSecond.beatmap!.MetadataSettings.version);
        Assert.Contains("see [A] (150, 4/4)", missingInSecond.message);

        var missingInFirst = Assert.Single(Run(one, two));
        Assert.Equal("A", missingInFirst.beatmap!.MetadataSettings.version);
    }

    [Fact]
    public void MeterAndBpm_AreCompared()
    {
        var issues = Run(
            [TestTimingPoints.Uninherited(0, 500)],
            [TestTimingPoints.Uninherited(0, 400, meter: 3)]
        );

        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
        Assert.Contains(
            issues,
            issue => issue.message.Contains("meter signature (3/4), see [A] (4/4)")
        );
        Assert.Contains(issues, issue => issue.message.Contains("BPM (150), see [A] (120)"));
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public void DecimalDifference_IsOnlyMinor()
    {
        var issues = Run(
            [TestTimingPoints.Uninherited(1000, 500)],
            [TestTimingPoints.Uninherited(1000.4, 500)]
        );

        Assert.NotEmpty(issues);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Minor, issue.level));
    }
}
