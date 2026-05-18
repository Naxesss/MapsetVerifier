using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects;

public class SliderBezierPathRegressionTests
{
    [Fact]
    public void RedAnchorMicroSegments_BuildPathBeyondHead()
    {
        var beatmap = ParseBeatmap(
            hitObjects:
            [
                // Duplicate anchors split into short linear segments along the top of the playfield.
                "100,200,1000,6,0,B|120:200|120:200|80:200|80:200|50:200|50:200|10:200|10:200,1,80,0:0:0:0:",
            ]
        );
        var slider = Assert.IsType<Slider>(beatmap.HitObjects[0]);

        Assert.True(slider.RedAnchorPositions.Count >= 3);
        Assert.True(slider.PathPxPositions.Count > 10);
        Assert.NotEqual(slider.Position, slider.PathPxPositions[^1]);
        Assert.NotEqual(
            slider.Position,
            slider.GetPathPosition(slider.time + slider.GetCurveDuration() / 2)
        );
    }

    [Fact]
    public void Bezier_EndStopsAtPixelLengthNotLastControlPoint()
    {
        var beatmap = ParseBeatmap(
            circleSize: 5,
            sliderMultiplier: 2,
            timingPoints: ["476,405.405405405405,4,2,1,35,1,0"],
            hitObjects:
            [
                "405,347,133854,6,0,B|433:311|371:274|292:289|281:429,1,200,2|0,3:2|0:0,0:0:0:0:",
            ]
        );
        var slider = Assert.IsType<Slider>(beatmap.HitObjects[0]);

        Assert.Equal(200, slider.PixelLength, precision: 0);
        Assert.NotEqual(slider.NodePositions[^1], slider.EndPosition);
        Assert.True(slider.EndPosition.Y < slider.NodePositions[^1].Y);
    }

    [Fact]
    public void SimpleBezier_SamplesAlongCurve()
    {
        var beatmap = ParseBeatmap(
            hitObjects: ["256,192,1000,6,0,B|300:220|350:192,1,120,0:0:0:0:"]
        );
        var slider = Assert.IsType<Slider>(beatmap.HitObjects[0]);

        Assert.True(slider.PathPxPositions.Count > 50);
        Assert.Contains(slider.PathPxPositions, p => p.X > 300);
        Assert.Contains(slider.PathPxPositions, p => p.Y > 200);
    }

    [Fact]
    public void LinearSlider_PathPxReachesDeclaredLength()
    {
        var beatmap = ParseBeatmap(hitObjects: ["256,192,1000,6,0,L|350:192,1,100,0:0:0:0:"]);
        var slider = Assert.IsType<Slider>(beatmap.HitObjects[0]);

        Assert.InRange(slider.PathPxPositions.Count, 80, 120);
        Assert.InRange(slider.EndPosition.X, slider.Position.X + 90, slider.Position.X + 110);
        Assert.Equal(192, slider.EndPosition.Y, precision: 0);
        Assert.Equal(slider.EndPosition, slider.PathPxPositions[^1]);
    }

    private static Beatmap ParseBeatmap(
        IEnumerable<string> hitObjects,
        float circleSize = 4,
        double sliderMultiplier = 1.4,
        IEnumerable<string>? timingPoints = null
    )
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            "MapsetVerifierParserTests",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(rootPath);

        try
        {
            File.WriteAllText(
                Path.Combine(rootPath, "test.osu"),
                string.Join(
                    "\n",
                    "osu file format v14",
                    "[General]",
                    "AudioFilename:",
                    "Mode: 0",
                    "[Metadata]",
                    "Title:Slider Path",
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
                )
            );

            return new BeatmapSet(rootPath).Beatmaps[0];
        }
        finally
        {
            try
            {
                Directory.Delete(rootPath, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
