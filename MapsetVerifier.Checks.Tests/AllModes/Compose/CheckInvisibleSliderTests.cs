using MapsetVerifier.Checks.AllModes.Compose;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Compose;

public class CheckInvisibleSliderTests
{
    [Fact]
    public void SliderWithNoCurvePoints_Flags()
    {
        // "L" with nothing after the pipe: no curve points at all, e.g.
        // https://github.com/Naxesss/MapsetVerifier/issues/21
        var hitObjects = new List<string> { "146,189,13070,2,0,L,1,47.9999985351563" };

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckInvisibleSlider>("Test");

        var issue = Assert.Single(issues);
        Assert.Contains("no curve points", issue.message);
    }

    [Fact]
    public void SliderWithEmptyCurvePointEntries_Flags()
    {
        // Empty entries between pipes still resolve to zero actual curve points.
        var hitObjects = new List<string> { "146,189,13070,2,0,L||,1,47.9999985351563" };

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckInvisibleSlider>("Test");

        var issue = Assert.Single(issues);
        Assert.Contains("no curve points", issue.message);
    }

    [Fact]
    public void SliderWithCurvePoints_DoesNotFlag()
    {
        var hitObjects = new List<string> { "113,85,11274,6,0,P|151:85|201:121,1,80" };

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckInvisibleSlider>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void SliderWithOnlyStartAndEnd_DoesNotFlag()
    {
        // Minimal valid slider: one explicit curve point marking the end,
        // giving a start node + end node (NodePositions.Count == 2).
        var hitObjects = new List<string> { "146,189,13070,2,0,L|300:189,1,150" };

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckInvisibleSlider>("Test");

        Assert.Empty(issues);
    }

    private static string BuildOsu(IEnumerable<string> hitObjects)
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            $"Mode: {(int)Beatmap.Mode.Standard}",
            "[Metadata]",
            "Title:Invisible Slider",
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
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
        };

        lines.AddRange(hitObjects);

        return string.Join("\n", lines);
    }
}
