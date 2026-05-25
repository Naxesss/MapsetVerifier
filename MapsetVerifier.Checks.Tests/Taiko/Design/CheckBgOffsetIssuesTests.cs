using MapsetVerifier.Checks.Taiko.Design;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Design;

public class CheckBgOffsetIssuesTests
{
    [Fact]
    public void FlagsInconsistentBackgroundOffsetsAcrossTaikoDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("kantan.osu", BuildTaikoOsu("Kantan", "0,0,\"bg.png\",0,20")),
                ("oni.osu", BuildTaikoOsu("Oni", "0,0,\"bg.png\",0,0")),
            ],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
        Assert.Contains(
            issues,
            issue => issue.message.Contains("Kantan") && issue.message.Contains("0, 20")
        );
        Assert.Contains(
            issues,
            issue => issue.message.Contains("Oni") && issue.message.Contains("0, 0")
        );
    }

    [Fact]
    public void DoesNotFlagConsistentYOnlyBackgroundOffsets()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("kantan.osu", BuildTaikoOsu("Kantan", "0,0,\"bg.png\",0,20")),
                ("futsuu.osu", BuildTaikoOsu("Futsuu", "0,0,\"bg.png\",0,20")),
            ],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsNonZeroXOffsetOnIndividualDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("oni.osu", BuildTaikoOsu("Oni", "0,0,\"bg.png\",12,20"))],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Equal("Oni", issue.beatmap?.MetadataSettings.version);
        Assert.Contains("X offset of 12", issue.message);
        Assert.Contains("Ensure this is intentional", issue.message);
    }

    [Fact]
    public void FlagsNonZeroXOffsetEvenWhenConsistentAcrossDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("kantan.osu", BuildTaikoOsu("Kantan", "0,0,\"bg.png\",5,20")),
                ("futsuu.osu", BuildTaikoOsu("Futsuu", "0,0,\"bg.png\",5,20")),
            ],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        Assert.Equal(2, issues.Count);
        Assert.All(
            issues,
            issue =>
            {
                Assert.Equal(Issue.Level.Warning, issue.level);
                Assert.Contains("X offset of 5", issue.message);
            }
        );
    }

    [Fact]
    public void DoesNotCompareAgainstNonTaikoBeatmaps()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("taiko.osu", BuildTaikoOsu("Oni", "0,0,\"bg.png\",0,0")),
                ("standard.osu", BuildOsu(Beatmap.Mode.Standard, "Hard", "0,0,\"bg.png\",0,20")),
            ],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        Assert.Empty(issues);
    }

    private static string BuildTaikoOsu(string version, string backgroundLine) =>
        BuildOsu(Beatmap.Mode.Taiko, version, backgroundLine);

    private static string BuildOsu(Beatmap.Mode mode, string version, string backgroundLine)
    {
        return string.Join(
            "\n",
            [
                "osu file format v14",
                "[General]",
                "AudioFilename: audio.mp3",
                $"Mode: {(int)mode}",
                "[Metadata]",
                "Title:Background Offset Test",
                "Artist:MapsetVerifier",
                "Creator:Tests",
                $"Version:{version}",
                "[Difficulty]",
                "HPDrainRate:5",
                "CircleSize:4",
                "OverallDifficulty:5",
                "ApproachRate:5",
                "SliderMultiplier:1.4",
                "SliderTickRate:1",
                "[Events]",
                backgroundLine,
                "[TimingPoints]",
                "0,500,4,2,0,100,1,0",
                "[HitObjects]",
                "256,192,1000,1,0,0:0:0:0:",
            ]
        );
    }
}
