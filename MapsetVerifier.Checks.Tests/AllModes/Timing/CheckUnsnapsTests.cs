using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckUnsnapsTests
{
    [Fact]
    public void DoesNotFlagSliderTailSnappedToUpcomingMisalignedRedLine()
    {
        // Regression for chouchou merged syrups. - Havfrue (pearto) [test]: slider tails snapped to an
        // upcoming misaligned red line can land fractionally before it due to pixel-length math.
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,0,6,0,L|300:192,1,59.16,2|2,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void StillFlagsUnsnappedCircle()
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

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.NotEmpty(issues);
    }

    [Fact]
    public void StillFlagsSliderTailWellBeforeUpcomingMisalignedRedLine()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,0,6,0,L|300:192,1,50,2|2,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.Contains(issues, issue => issue.message.Contains("Slider tail"));
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
            "Title:Unsnap Test",
            "Artist:MapsetVerifier",
            "Creator:Tests",
            "Version:Test",
            "[Difficulty]",
            "CircleSize:4",
            "HPDrainRate:5",
            "OverallDifficulty:5",
            "ApproachRate:5",
            "SliderMultiplier:1.7",
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
