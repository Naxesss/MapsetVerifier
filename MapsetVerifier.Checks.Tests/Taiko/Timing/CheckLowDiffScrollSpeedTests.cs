using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Timing;

public class CheckLowDiffScrollSpeedTests
{
    [Fact]
    public void DoesNotFlagConstantScrollSpeed()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "easy.osu",
                BuildTaikoOsu(
                    "Easy",
                    timingPoints: ["0,500,4,2,0,100,1,0", "10000,500,4,2,0,100,0,0"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckLowDiffScrollSpeed>("Easy");

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagBpmNormalizationThatPreservesScrollSpeed()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "easy.osu",
                BuildTaikoOsu(
                    "Easy",
                    timingPoints:
                    [
                        "0,500,4,2,0,100,1,0",
                        "10000,375,4,2,0,100,1,0",
                        "10000,-133,4,2,0,100,0,0",
                    ],
                    hitObjects:
                    [
                        "256,192,0,1,0,0:0:0:0:",
                        "256,192,20000,1,0,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckLowDiffScrollSpeed>("Easy");

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsNonDominantSliderVelocityChange()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "normal.osu",
                BuildTaikoOsu(
                    "Normal",
                    timingPoints:
                    [
                        "0,500,4,2,0,100,1,0",
                        "20000,500,4,2,0,100,0,0",
                        "20000,-80,4,2,0,100,0,0",
                    ],
                    hitObjects:
                    [
                        "256,192,0,1,0,0:0:0:0:",
                        "256,192,30000,1,0,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckLowDiffScrollSpeed>("Normal");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("1.25", issue.message);
        Assert.Contains("(1x at this BPM)", issue.message);
    }

    [Fact]
    public void DoesNotFlagSliderVelocityChangeWithNoObjectsOrBarlinesInSection()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "easy.osu",
                BuildTaikoOsu(
                    "Easy",
                    timingPoints:
                    [
                        "0,500,4,2,0,100,1,0",
                        "11000,-80,4,2,0,100,0,0",
                        "11500,-100,4,2,0,100,0,0",
                    ],
                    hitObjects: ["256,192,8000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckLowDiffScrollSpeed>("Easy");

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsSliderVelocityChangeInSectionWithBarlinesButNoObjects()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "easy.osu",
                BuildTaikoOsu(
                    "Easy",
                    timingPoints:
                    [
                        "0,500,4,2,0,100,1,0",
                        "10000,-80,4,2,0,100,0,0",
                        "15000,-100,4,2,0,100,0,0",
                    ],
                    hitObjects:
                    [
                        "256,192,0,1,0,0:0:0:0:",
                        "256,192,20000,1,0,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckLowDiffScrollSpeed>("Easy");

        var issue = Assert.Single(issues);
        Assert.Contains("00:10:000", issue.message);
    }

    [Fact]
    public void DoesNotInspectHardDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "hard.osu",
                BuildTaikoOsu(
                    "Hard",
                    timingPoints:
                    [
                        "0,500,4,2,0,100,1,0",
                        "20000,500,4,2,0,100,0,0",
                        "20000,-80,4,2,0,100,0,0",
                    ],
                    hitObjects:
                    [
                        "256,192,0,1,0,0:0:0:0:",
                        "256,192,30000,1,0,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var beatmap = context.GetBeatmap("Hard");
        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Hard;

        var issues = new CheckLowDiffScrollSpeed().GetIssues(beatmap).ToList();

        Assert.Empty(issues);
    }

    /// <summary>
    ///     Inherited line at 02:04:141 changes SV before the downbeat at 02:04:184 (not on the line itself).
    ///     Timing excerpted from a Kantan map where the section has no notes but a visible barline.
    /// </summary>
    [Fact]
    public void FlagsInheritedLineAffectingOffBarlineDownbeat()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "easy.osu",
                BuildTaikoOsu(
                    "Easy",
                    timingPoints:
                    [
                        "2128,342.857142857143,4,1,0,50,1,0",
                        "120756,-142.857142857143,4,1,0,50,0,0",
                        "121270,-100,4,1,0,50,0,0",
                        "121356,-20,4,1,0,50,0,0",
                        "121528,-100,4,1,0,50,0,0",
                        "122813,171.428571428571,4,1,0,100,1,1",
                        "122813,-200,4,1,0,100,0,1",
                        "124141,-142.857142857143,4,1,0,100,0,1",
                        "124270,-200,4,1,0,100,0,1",
                    ],
                    hitObjects:
                    [
                        "256,192,120070,1,0,0:0:0:0:",
                        "256,192,120756,1,0,0:0:0:0:",
                        "256,192,123670,1,0,0:0:0:0:",
                        "256,192,124870,1,0,0:0:0:0:",
                        "256,192,127613,1,0,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var beatmap = context.GetBeatmap("Easy");

        const double inheritedLineMs = 124141;
        const double barlineMs = 124184;

        Assert.True(TaikoUtils.IsOnBarline(beatmap, barlineMs));
        Assert.False(TaikoUtils.IsOnBarline(beatmap, inheritedLineMs));

        var issues = context.RunBeatmapCheck<CheckLowDiffScrollSpeed>("Easy");

        // Warning timestamp is the inherited line; scroll applies to the barline at 02:04:184.
        Assert.Contains(issues, issue => issue.message.Contains("02:04:141"));
    }

    private static string BuildTaikoOsu(
        string version,
        IEnumerable<string> timingPoints,
        IEnumerable<string>? hitObjects = null
    ) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename:",
            "Mode: 1",
            "[Metadata]",
            "Title:Easy Scroll Speed",
            "Artist:Tests",
            "Creator:Tests",
            $"Version:{version}",
            "[Difficulty]",
            "CircleSize:4",
            "HPDrainRate:5",
            "OverallDifficulty:5",
            "ApproachRate:5",
            "SliderMultiplier:1.4",
            "SliderTickRate:1",
            "[Events]",
            "[TimingPoints]",
            string.Join('\n', timingPoints),
            "[HitObjects]",
            string.Join('\n', hitObjects ?? ["256,192,1000,1,0,0:0:0:0:"])
        );
}
