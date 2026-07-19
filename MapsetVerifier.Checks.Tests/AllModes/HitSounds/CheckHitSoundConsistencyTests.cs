using MapsetVerifier.Checks.AllModes.HitSounds;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.HitSounds;

public class CheckHitSoundConsistencyTests
{
    [Fact]
    public void MissingAddition_FlaggedAsWarning_WhenMajorityOfOtherDiffsHaveIt()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", BuildOsu(Beatmap.Mode.Standard, "A", ["256,192,1000,1,8,0:0:0:0:"])),
            ("b.osu", BuildOsu(Beatmap.Mode.Standard, "B", ["256,192,1000,1,0,0:0:0:0:"])),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckHitSoundConsistency>();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Equal("B", issue.beatmap?.MetadataSettings.version);
        Assert.Contains("Clap", issue.message);
    }

    [Fact]
    public void MissingAddition_FlaggedAsMinor_WhenMinorityOfOtherDiffsHaveIt()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", BuildOsu(Beatmap.Mode.Standard, "A", ["256,192,1000,1,8,0:0:0:0:"])),
            ("b.osu", BuildOsu(Beatmap.Mode.Standard, "B", ["256,192,1000,1,0,0:0:0:0:"])),
            ("c.osu", BuildOsu(Beatmap.Mode.Catch, "C", ["256,192,1000,1,0,0:0:0:0:"])),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckHitSoundConsistency>();

        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Minor, issue.level));
        Assert.Contains(issues, issue => issue.beatmap?.MetadataSettings.version == "B");
        Assert.Contains(issues, issue => issue.beatmap?.MetadataSettings.version == "C");
    }

    [Fact]
    public void ConsistentHitSounds_ProducesNoIssues()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", BuildOsu(Beatmap.Mode.Standard, "A", ["256,192,1000,1,8,0:0:0:0:"])),
            ("b.osu", BuildOsu(Beatmap.Mode.Catch, "B", ["256,192,1000,1,8,0:0:0:0:"])),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckHitSoundConsistency>();

        Assert.Empty(issues);
    }

    [Fact]
    public void SliderBodyAddition_FlaggedAsMinor()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "a.osu",
                BuildOsu(
                    Beatmap.Mode.Standard,
                    "A",
                    ["256,192,1000,2,2,L|256:300,1,120,0|0,0:0|0:0,0:0:0:0:"]
                )
            ),
            ("b.osu", BuildOsu(Beatmap.Mode.Standard, "B", ["256,192,1000,1,0,0:0:0:0:"])),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckHitSoundConsistency>();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Equal("A", issue.beatmap?.MetadataSettings.version);
        Assert.Contains("sliderbody", issue.message);
    }

    [Fact]
    public void DivergentDifficulty_ExcludedAndFlaggedAsUniqueHitSounds()
    {
        var times = new[] { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000 };
        var consistentObjects = times.Select(time => $"256,192,{time},1,2,0:0:0:0:").ToArray();
        var divergentObjects = times.Select(time => $"256,192,{time},1,0,0:0:0:0:").ToArray();

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("c0.osu", BuildOsu(Beatmap.Mode.Standard, "C0", consistentObjects)),
            ("c1.osu", BuildOsu(Beatmap.Mode.Standard, "C1", consistentObjects)),
            ("c2.osu", BuildOsu(Beatmap.Mode.Standard, "C2", consistentObjects)),
            ("c3.osu", BuildOsu(Beatmap.Mode.Standard, "C3", consistentObjects)),
            ("d.osu", BuildOsu(Beatmap.Mode.Standard, "D", divergentObjects)),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckHitSoundConsistency>();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Equal("D", issue.beatmap?.MetadataSettings.version);
        Assert.Contains("own hit sounding", issue.message);
    }

    [Fact]
    public void SingleRelevantDifficulty_ProducesNoIssues()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("mania.osu", BuildOsu(Beatmap.Mode.Mania, "Main", ["256,192,1000,1,8,0:0:0:0:"])),
            (
                "standard.osu",
                BuildOsu(Beatmap.Mode.Standard, "Easy", ["256,192,1000,1,0,0:0:0:0:"])
            ),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckHitSoundConsistency>();

        Assert.Empty(issues);
    }

    private static string BuildOsu(
        Beatmap.Mode mode,
        string version,
        IEnumerable<string> hitObjects
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            $"Mode: {(int)mode}",
            "[Metadata]",
            "Title:Hit Sound Consistency",
            "Artist:MapsetVerifier",
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
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
        };

        lines.AddRange(hitObjects);

        return string.Join("\n", lines);
    }
}
