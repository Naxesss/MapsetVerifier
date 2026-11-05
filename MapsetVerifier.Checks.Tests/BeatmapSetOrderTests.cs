using MapsetVerifier.Checks.AllModes.Timing;
using Xunit;

namespace MapsetVerifier.Checks.Tests;

/// <summary>
/// Beatmaps are parsed in parallel, so any order that isn't fully decided by the sort keys would
/// change between runs. Set checks that compare against "the first" or "the nearest" difficulty
/// would then point at a different difficulty each time.
/// </summary>
public class BeatmapSetOrderTests
{
    [Fact]
    public void TiedDifficulties_AreOrderedByName()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            new[] { "Charlie", "Alpha", "Bravo" }.Select(name =>
                ($"{name}.osu", Difficulty(name, "256,192,1000,1,0,0:0:0:0:"))
            ),
            extraFiles: ["audio.mp3"]
        );

        var names = context.BeatmapSet.Beatmaps.Select(b => b.MetadataSettings.version);

        Assert.Equal(["Alpha", "Bravo", "Charlie"], names);
    }

    [Fact]
    public void SnapConsistency_ComparesAgainstTheNearestDifficultyInTheSpread()
    {
        // Both higher difficulties use 1/3 at 01:333 where the lower one uses 1/2 at 01:250, and
        // they tie on everything but their name, so the first by name should be reported.
        string[] higherObjects =
        [
            "100,192,0,1,0,0:0:0:0:",
            "400,192,333,1,0,0:0:0:0:",
            "100,192,667,1,0,0:0:0:0:",
            "400,192,1000,1,0,0:0:0:0:",
            "100,192,1333,1,0,0:0:0:0:",
            "400,192,1667,1,0,0:0:0:0:",
            "100,192,2000,1,0,0:0:0:0:",
        ];
        string[] lowerObjects =
        [
            "256,192,0,1,0,0:0:0:0:",
            "256,192,1000,1,0,0:0:0:0:",
            "256,192,1250,1,0,0:0:0:0:",
            "256,192,2000,1,0,0:0:0:0:",
        ];

        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("beta.osu", Difficulty("Beta", higherObjects)),
                ("normal.osu", Difficulty("Normal", lowerObjects)),
                ("alpha.osu", Difficulty("Alpha", higherObjects)),
            ],
            extraFiles: ["audio.mp3"]
        );

        var issue = Assert.Single(
            context.RunBeatmapSetCheck<CheckWrongSnapping>(),
            issue => issue.message.Contains("Different snapping")
        );

        Assert.Contains("Alpha", issue.message);
        Assert.DoesNotContain("Beta", issue.message);
    }

    private static string Difficulty(string version, params string[] hitObjects) =>
        new OsuBuilder().Version(version).WithDefaultTiming().HitObjects(hitObjects).Build();
}
