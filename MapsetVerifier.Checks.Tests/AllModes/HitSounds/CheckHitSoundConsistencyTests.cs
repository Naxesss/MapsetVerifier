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
            (
                "a.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("A")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000, hitSound: 8))
                    .Build()
            ),
            (
                "b.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("B")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000))
                    .Build()
            ),
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
            (
                "a.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("A")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000, hitSound: 8))
                    .Build()
            ),
            (
                "b.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("B")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000))
                    .Build()
            ),
            (
                "c.osu",
                new OsuBuilder()
                    .Mode(Beatmap.Mode.Catch)
                    .Title("Hit Sound Consistency")
                    .Version("C")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000))
                    .Build()
            ),
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
            (
                "a.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("A")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000, hitSound: 8))
                    .Build()
            ),
            (
                "b.osu",
                new OsuBuilder()
                    .Mode(Beatmap.Mode.Catch)
                    .Title("Hit Sound Consistency")
                    .Version("B")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000, hitSound: 8))
                    .Build()
            ),
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
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("A")
                    .WithDefaultTiming()
                    .HitObjects("256,192,1000,2,2,L|256:300,1,120,0|0,0:0|0:0,0:0:0:0:")
                    .Build()
            ),
            (
                "b.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("B")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000))
                    .Build()
            ),
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
        var consistentObjects = times
            .Select(time => TestHitObjects.Circle(time, hitSound: 2))
            .ToArray();
        var divergentObjects = times.Select(time => TestHitObjects.Circle(time)).ToArray();

        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "c0.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("C0")
                    .WithDefaultTiming()
                    .HitObjects(consistentObjects)
                    .Build()
            ),
            (
                "c1.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("C1")
                    .WithDefaultTiming()
                    .HitObjects(consistentObjects)
                    .Build()
            ),
            (
                "c2.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("C2")
                    .WithDefaultTiming()
                    .HitObjects(consistentObjects)
                    .Build()
            ),
            (
                "c3.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("C3")
                    .WithDefaultTiming()
                    .HitObjects(consistentObjects)
                    .Build()
            ),
            (
                "d.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("D")
                    .WithDefaultTiming()
                    .HitObjects(divergentObjects)
                    .Build()
            ),
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
            (
                "mania.osu",
                new OsuBuilder()
                    .Mode(Beatmap.Mode.Mania)
                    .Title("Hit Sound Consistency")
                    .Version("Main")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000, hitSound: 8))
                    .Build()
            ),
            (
                "standard.osu",
                new OsuBuilder()
                    .Title("Hit Sound Consistency")
                    .Version("Easy")
                    .WithDefaultTiming()
                    .HitObjects(TestHitObjects.Circle(1000))
                    .Build()
            ),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckHitSoundConsistency>();

        Assert.Empty(issues);
    }
}
