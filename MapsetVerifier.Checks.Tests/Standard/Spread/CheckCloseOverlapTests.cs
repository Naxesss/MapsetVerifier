using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckCloseOverlapTests
{
    private static List<Issue> Run(int distance, int ms, string version = "Easy")
    {
        // The radius at CS 5 is 32, so circles touch at 64 px.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Version(version)
                .CircleSize(5)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Circle(1000, 100, 100),
                    TestHitObjects.Circle(1000 + ms, 100 + distance, 100)
                )
        );

        return context.RunBeatmapSetCheck<CheckCloseOverlap>();
    }

    [Theory]
    [InlineData(64, 124, Issue.Level.Problem)]
    [InlineData(64, 125, Issue.Level.Warning)]
    [InlineData(64, 187, Issue.Level.Warning)]
    [InlineData(64, 188, null)]
    [InlineData(63, 100, null)] // Overlapping.
    public void CloseInTimeWithoutOverlap(int distance, int ms, Issue.Level? expected)
    {
        var issues = Run(distance, ms);

        Assert.Equal(expected, issues.SingleOrDefault()?.level);
        Assert.All(
            issues,
            issue => Assert.False(issue.AppliesToDifficulty(Beatmap.Difficulty.Hard))
        );
    }

    [Theory]
    [InlineData("Normal", true)]
    [InlineData("Hard", false)]
    public void Normals_OnlyWithoutAnEasy(string version, bool expected)
    {
        Assert.Equal(expected, Run(64, 100, version).Count == 1);
    }

    [Fact]
    public void Normals_WithAnEasy_AreLeftOut()
    {
        string Build(string version) =>
            new OsuBuilder()
                .Version(version)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Circle(1000, 100, 100),
                    TestHitObjects.Circle(1100, 400, 100)
                )
                .Build();

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", Build("Normal")),
            ("b.osu", Build("Easy")),
        ]);

        Assert.Equal(
            "Easy",
            Assert
                .Single(context.RunBeatmapSetCheck<CheckCloseOverlap>())
                .beatmap!.MetadataSettings.version
        );
    }
}
