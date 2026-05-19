using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckBuraiRegressionTests
{
    [Fact]
    public void DoesNotFlagColinearRedAnchorChain()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    hitObjects:
                    [
                        "100,200,1000,6,0,B|150:200|150:200|200:200|200:200|250:200|250:200|300:200|300:200|350:200,1,250,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var slider = Assert.IsType<Slider>(context.GetBeatmap("Test").HitObjects[0]);
        Assert.True(slider.RedAnchorPositions.Count >= 3);
        Assert.True(CheckBurai.ComputeBuraiScore(slider) <= CheckBurai.PotentiallyBuraiThreshold);

        var issues = context.RunBeatmapCheck<CheckBurai>("Test");
        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagSmoothMultiPointBezier()
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
        Assert.True(CheckBurai.ComputeBuraiScore(slider) <= CheckBurai.PotentiallyBuraiThreshold);

        var issues = context.RunBeatmapCheck<CheckBurai>("Test");
        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsDefinitelyBuraiSlider()
    {
        const string hitObject =
            "256,192,1000,6,0,B|450:192|450:50|50:50|50:192|258:192|450:192|450:50|50:50|50:192|258:192,1,1600,0:0:0:0:";

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects: [hitObject])),
        ]);

        var slider = Assert.IsType<Slider>(context.GetBeatmap("Test").HitObjects[0]);
        var score = CheckBurai.ComputeBuraiScore(slider);

        Assert.True(
            score > CheckBurai.DefinitelyBuraiThreshold,
            $"Expected definite burai score, got {score}."
        );

        var issues = context.RunBeatmapCheck<CheckBurai>("Test");

        Assert.Contains(
            issues,
            issue =>
                issue.message.Contains("Burai", StringComparison.OrdinalIgnoreCase)
                && !issue.message.Contains("Potentially", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void FlagsPotentiallyBuraiSlider()
    {
        const string hitObject =
            "256,192,1000,6,0,B|450:192|450:50|50:50|50:192|260:192,1,700,0:0:0:0:";

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects: [hitObject])),
        ]);

        var slider = Assert.IsType<Slider>(context.GetBeatmap("Test").HitObjects[0]);
        var score = CheckBurai.ComputeBuraiScore(slider);

        Assert.InRange(score, CheckBurai.PotentiallyBuraiThreshold, CheckBurai.DefinitelyBuraiThreshold);

        var issues = context.RunBeatmapCheck<CheckBurai>("Test");

        Assert.Contains(
            issues,
            issue => issue.message.Contains("Potentially burai", StringComparison.OrdinalIgnoreCase)
        );
        Assert.DoesNotContain(
            issues,
            issue =>
                issue.message.Contains("Burai", StringComparison.OrdinalIgnoreCase)
                && !issue.message.Contains("Potentially", StringComparison.OrdinalIgnoreCase)
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
            "Title:Burai Regression",
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
