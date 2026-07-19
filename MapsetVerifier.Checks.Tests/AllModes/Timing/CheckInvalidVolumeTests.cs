using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckInvalidVolumeTests
{
    [Theory]
    [InlineData(105)]
    [InlineData(-1)]
    [InlineData(150)]
    [InlineData(0)]
    [InlineData(4)]
    public void FlagsVolumeOutsideEditorRange(float volume)
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints:
                    [
                        $"0,500,4,2,0,{volume.ToString(System.Globalization.CultureInfo.InvariantCulture)},1,0",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckInvalidVolume>("Test");

        Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issues[0].level);
        Assert.Contains(
            $"{volume.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}%",
            issues[0].message
        );
    }

    [Theory]
    [InlineData(5)]
    [InlineData(100)]
    [InlineData(50)]
    public void DoesNotFlagValidVolume(float volume)
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints:
                    [
                        $"0,500,4,2,0,{volume.ToString(System.Globalization.CultureInfo.InvariantCulture)},1,0",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckInvalidVolume>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsEachInvalidLine()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints:
                    [
                        "0,500,4,2,0,100,1,0",
                        "1000,500,4,2,0,105,0,0",
                        "2000,500,4,2,0,-5,0,0",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckInvalidVolume>("Test");

        Assert.Equal(2, issues.Count);
    }

    private static string BuildOsu(IEnumerable<string> timingPoints) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename:",
            "Mode: 0",
            "[Metadata]",
            "Title:Invalid Volume",
            "Artist:Tests",
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
            string.Join('\n', timingPoints),
            "[HitObjects]"
        );
}
