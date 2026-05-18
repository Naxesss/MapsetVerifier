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

    private static Beatmap ParseBeatmap(IEnumerable<string> hitObjects)
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
                    "CircleSize:4",
                    "HPDrainRate:5",
                    "OverallDifficulty:5",
                    "ApproachRate:5",
                    "SliderMultiplier:1.4",
                    "SliderTickRate:1",
                    "[Events]",
                    "[TimingPoints]",
                    "0,500,4,2,0,100,1,0",
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
