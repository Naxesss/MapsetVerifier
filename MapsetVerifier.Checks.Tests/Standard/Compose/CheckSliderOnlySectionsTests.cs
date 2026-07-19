using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckSliderOnlySectionsTests
{
    [Fact]
    public void LongSliderOnlySection_FlagsMinor()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 7000; time += 1000)
            hitObjects.Add(Slider(time));
        hitObjects.Add(Circle(9000));

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("8 objects", issue.message);
    }

    [Fact]
    public void TrailingSliderOnlySection_WithNoFollowingObject_FlagsMinor()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 7000; time += 1000)
            hitObjects.Add(Slider(time));

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        Assert.Single(issues);
    }

    [Fact]
    public void FewSliders_DoesNotFlag()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 4000; time += 1000)
            hitObjects.Add(Slider(time));
        hitObjects.Add(Circle(6000));

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void ManySliders_ButShortDuration_DoesNotFlag()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 700; time += 100)
            hitObjects.Add(Slider(time));
        hitObjects.Add(Circle(1000));

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void CircleBreaksUpSections_NeitherSectionFlags()
    {
        var hitObjects = new List<string>();
        for (var time = 0; time <= 3000; time += 1000)
            hitObjects.Add(Slider(time));
        hitObjects.Add(Circle(4000));
        for (var time = 5000; time <= 8000; time += 1000)
            hitObjects.Add(Slider(time));
        hitObjects.Add(Circle(9000));

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildOsu(hitObjects)),
        ]);

        var issues = context.RunBeatmapCheck<CheckSliderOnlySections>("Test");

        Assert.Empty(issues);
    }

    private static string Slider(int time) =>
        $"256,192,{time},2,0,L|256:300,1,100,0|0,0:0|0:0,0:0:0:0:";

    private static string Circle(int time) => $"256,192,{time},1,0,0:0:0:0:";

    private static string BuildOsu(IEnumerable<string> hitObjects)
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            $"Mode: {(int)Beatmap.Mode.Standard}",
            "[Metadata]",
            "Title:Slider Only Sections",
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
