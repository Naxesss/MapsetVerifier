using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Timing;

public class CheckInconsistentBarLinesTests
{
    private static List<Issue> Run(int firstEffects, int secondEffects, double secondOffset = 1000)
    {
        string Build(string version, double offset, int effects) =>
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .Version(version)
                .TimingPoints(TestTimingPoints.Uninherited(offset, 500, effects: effects))
                .HitObjects(TestHitObjects.Circle(2000))
                .Build();

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", Build("A", 1000, firstEffects)),
            ("b.osu", Build("B", secondOffset, secondEffects)),
        ]);

        return context.RunBeatmapSetCheck<CheckInconsistentBarLines>();
    }

    [Fact]
    public void OmittedInOnlyOne_IsAProblem()
    {
        Assert.Empty(Run(8, 8));
        Assert.Empty(Run(0, 1)); // Kiai isn't a bar line.

        var issue = Assert.Single(Run(8, 0));
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Equal("B", issue.beatmap!.MetadataSettings.version);
    }

    [Fact]
    public void OtherLines_AreNotCompared()
    {
        Assert.Empty(Run(8, 0, 1500));
        Assert.Single(Run(8, 0, 1000.5));
    }
}
