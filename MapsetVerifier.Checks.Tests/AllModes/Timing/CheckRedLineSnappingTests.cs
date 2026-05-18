using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckRedLineSnappingTests
{
    [Fact]
    public void FlagsObjectSnappedToCurrentLineButNotUpcomingMisalignedRedLine()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,125,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckRedLineSnapping>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("upcoming red line at 00:00:175", issue.message);
    }

    [Fact]
    public void DoesNotFlagWhenUpcomingRedLineAlignsWithCurrentGrid()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "2000,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,1875,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckRedLineSnapping>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagWhenUpcomingRedLineIsBeyondLookahead()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "250,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,125,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckRedLineSnapping>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagWhenAlreadyUnsnappedOnCurrentTiming()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,127,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckRedLineSnapping>("Test");

        Assert.Empty(issues);
    }

    private static string BuildOsu(
        IEnumerable<string>? timingPoints = null,
        IEnumerable<string>? hitObjects = null
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 0",
            "[Metadata]",
            "Title:Red Line Snap",
            "Artist:MapsetVerifier",
            "Creator:Tests",
            "Version:Test",
            "[Difficulty]",
            "CircleSize:4",
            "HPDrainRate:5",
            "OverallDifficulty:5",
            "ApproachRate:5",
            "SliderMultiplier:1.4",
            "SliderTickRate:1",
            "[Events]",
            "[TimingPoints]",
        };

        lines.AddRange(timingPoints ?? ["0,500,4,2,0,100,1,0"]);
        lines.Add("[HitObjects]");
        lines.AddRange(hitObjects ?? ["256,192,1000,1,0,0:0:0:0:"]);

        return string.Join("\n", lines);
    }
}
