using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckOffscreenRegressionTests
{
    [Fact]
    public void FlagsLinearSliderTailOffscreenLeft()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects: ["256,192,1000,6,0,L|-40:192,1,296,0:0:0:0:"])),
        ]);

        var issues = context.RunBeatmapCheck<CheckOffscreen>("Test");

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("Slider tail", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void DoesNotFlagOnScreenLinearSlider()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects: ["256,192,1000,6,0,L|350:192,1,100,0:0:0:0:"])),
        ]);

        var issues = context
            .RunBeatmapCheck<CheckOffscreen>("Test")
            .Where(issue => issue.level == Issue.Level.Problem)
            .ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsRedAnchorMicroSegmentBezierOffscreen()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    hitObjects:
                    [
                        // Offscreen bulge left, tail returns on-screen so the body path is checked.
                        "100,200,1000,6,0,B|120:200|120:200|80:200|80:200|-50:200|-50:200|50:200|50:200,1,200,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var beatmap = context.GetBeatmap("Test");
        var slider = Assert.IsType<Slider>(beatmap.HitObjects[0]);

        Assert.True(slider.PathPxPositions.Count > 10);
        Assert.True(slider.PathPxPositions.Min(p => p.X) < -30);
        Assert.True(slider.EndPosition.X > 0);

        var issues = context.RunBeatmapCheck<CheckOffscreen>("Test");

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("Slider body", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void DoesNotFlagBezierWhenPlayableEndIsBeforeLastControlPoint()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    circleSize: 5,
                    sliderMultiplier: 2,
                    timingPoints: ["476,405.405405405405,4,2,1,35,1,0"],
                    hitObjects:
                    [
                        "405,347,133854,6,0,B|433:311|371:274|292:289|281:429,1,200,2|0,3:2|0:0,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var slider = Assert.IsType<Slider>(context.GetBeatmap("Test").HitObjects[0]);

        Assert.True(slider.EndPosition.Y < 396);

        var issues = context
            .RunBeatmapCheck<CheckOffscreen>("Test")
            .Where(issue => issue.level == Issue.Level.Problem)
            .ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void DenseBezierFixture_StillFlagsIssue26Case()
    {
        using var context = CheckTestContext.Create("OffscreenDenseBezier");
        var beatmap = context.GetBeatmap("test");
        var slider = Assert.IsType<Slider>(beatmap.HitObjects[0]);

        Assert.True(slider.PathPxPositions.Count > 100);
        Assert.True(slider.PathPxPositions.Min(p => p.X) < -28);

        var issues = context.RunBeatmapCheck<CheckOffscreen>("test");

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("00:53:942", StringComparison.Ordinal)
                && issue.message.Contains("Slider body", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void DenseBezierFixture_SanityObjectsStayClean()
    {
        using var context = CheckTestContext.Create("OffscreenDenseBezier");

        var issues = context
            .RunBeatmapCheck<CheckOffscreen>("test")
            .Where(issue => issue.level == Issue.Level.Problem)
            .ToList();

        Assert.All(
            issues,
            issue => Assert.Contains("00:53:942", issue.message, StringComparison.Ordinal)
        );
    }

    private static string BuildOsu(
        IEnumerable<string> hitObjects,
        float circleSize = 4,
        double sliderMultiplier = 1.4,
        IEnumerable<string>? timingPoints = null
    ) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename:",
            "Mode: 0",
            "[Metadata]",
            "Title:Offscreen Regression",
            "Artist:Tests",
            "Creator:Tests",
            "Version:Test",
            "[Difficulty]",
            $"CircleSize:{circleSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            "HPDrainRate:5",
            "OverallDifficulty:5",
            "ApproachRate:5",
            $"SliderMultiplier:{sliderMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            "SliderTickRate:1",
            "[Events]",
            "[TimingPoints]",
            string.Join('\n', timingPoints ?? ["0,500,4,2,0,100,1,0"]),
            "[HitObjects]",
            string.Join('\n', hitObjects)
        );
}
