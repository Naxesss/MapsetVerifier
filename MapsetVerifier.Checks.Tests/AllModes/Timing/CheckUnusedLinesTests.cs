using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckUnusedLinesTests
{
    // --- Uninherited (red) line cases ---

    [Fact]
    public void UninheritedLine_IdenticalToPrevious_OnDownbeat_FlagsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "8000,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,9000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("changes nothing.", issue.message);
    }

    [Fact]
    public void UninheritedLine_ChangesSamplesOnly_WithObjectInSection_FlagsProblemReplaceable()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "8000,500,4,2,0,50,1,0"],
                    hitObjects: ["256,192,8500,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains(
            "changes nothing that can't be changed with an inherited line",
            issue.message
        );
    }

    [Fact]
    public void UninheritedLine_OnlyOmitsBarLine_InStandard_FlagsWarning()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "8000,500,4,2,0,100,1,8"],
                    hitObjects: ["256,192,9000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("only omits the first barline", issue.message);
        Assert.Contains("otherwise remove the line", issue.message);
    }

    [Fact]
    public void UninheritedLine_OmitsBarLineAndChangesSamples_WithObjectInSection_FlagsWarningReplaceable()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "8000,500,4,2,0,50,1,8"],
                    hitObjects: ["256,192,8500,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("only omits the first barline", issue.message);
        Assert.Contains("otherwise replace with an inherited line", issue.message);
    }

    [Fact]
    public void UninheritedLine_OneMeasureAway_ResetsNightcoreCymbals_FlagsWarning()
    {
        // 2000ms is 1 measure (4 beats) away at 500ms/beat, but nightcore cymbals only
        // align every 4 measures (16 beats), so this resets the cymbal pattern.
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "2000,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,3000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("resets nightcore mod cymbals", issue.message);
    }

    [Fact]
    public void UninheritedLine_DifferentBpm_DoesNotFlag()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "1000,400,4,2,0,100,1,0"],
                    hitObjects: ["256,192,2000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void UninheritedLine_NotOnDownbeat_DoesNotFlag()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "1234,500,4,2,0,100,1,0"],
                    hitObjects: ["256,192,2000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void UninheritedLine_OnlyOmitsBarLine_InTaiko_IsSkippedEntirely()
    {
        // Omitting bar lines is common practice in taiko/mania, so an uninherited line whose
        // only effect is (un)omitting a bar line isn't flagged there, unlike in standard.
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "8000,500,4,2,0,100,1,8"],
                    hitObjects: ["256,192,9000,1,0,0:0:0:0:"],
                    mode: Beatmap.Mode.Taiko
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        Assert.Empty(issues);
    }

    // --- Inherited (green) line cases ---

    [Fact]
    public void InheritedLine_ChangesSvWithSliderStartingInSection_DoesNotFlag()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "1000,-50,4,2,0,100,0,0"],
                    hitObjects:
                    [
                        // An earlier slider is included alongside the one actually in this
                        // section, since a beatmap with only a single slider anywhere hits an
                        // unrelated edge case in the underlying binary search lookup.
                        "256,192,100,2,0,L|256:220,1,10,0|0,0:0|0:0,0:0:0:0:",
                        "256,192,1500,2,0,L|256:300,1,100,0|0,0:0|0:0,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void InheritedLine_ChangesSamplesWithObjectStartingBeforeButOverlappingSection_DoesNotFlag()
    {
        // Regression: a spinner (or slider) that started before this section but is still active
        // during it uses this section's sample settings for its ticks/tail, even though it didn't
        // start here.
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "2000,-100,4,2,0,50,0,0"],
                    hitObjects: ["256,192,1500,8,0,2500,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void InheritedLine_ChangesSvOnly_WithNoSlidersAnywhere_FlagsMinorSvOnly()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "1000,-50,4,2,0,100,0,0"],
                    hitObjects: ["256,192,500,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("Inherited line changes SV, but affects nothing", issue.message);
    }

    [Fact]
    public void InheritedLine_ChangesSamplesOnly_WithNoObjectsInSection_FlagsMinorSamplesOnly()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "5000,-100,4,2,0,30,0,0"],
                    hitObjects: ["256,192,1000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains(
            "Inherited line changes sample settings, but affects nothing",
            issue.message
        );
    }

    [Fact]
    public void InheritedLine_ChangesSvAndSamples_WithNothingInSection_FlagsMinorBoth()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "5000,-50,4,2,0,30,0,0"],
                    hitObjects: ["256,192,1000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains(
            "Inherited line changes SV and sample settings, but affects nothing",
            issue.message
        );
    }

    [Fact]
    public void InheritedLine_SameValuesAsUninheritedBefore_FlagsSameValuesMessage()
    {
        // A green line right after a red line, explicitly restating SV 1.0x and the same sample
        // settings the red line already implies, changes nothing at all - this should be reported
        // distinctly from a value that changed but merely doesn't apply to anything.
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "1000,-100,4,2,0,100,0,0"],
                    hitObjects: ["256,192,2000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("uses the same values as the previous timing line", issue.message);
        Assert.DoesNotContain("affects nothing", issue.message);
    }

    [Fact]
    public void InheritedLine_OnlyChangesKiai_DoesNotFlag()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "1000,-100,4,2,0,100,0,1"],
                    hitObjects: ["256,192,2000,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void InheritedLine_ChangesSvInMania_WithNoSliders_DoesNotFlag()
    {
        // Mania (and taiko) affect approach rate through SV, so SV changes are always
        // meaningful there even without any sliders present.
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                BuildOsu(
                    timingPoints: ["0,500,4,2,0,100,1,0", "1000,-50,4,2,0,100,0,0"],
                    hitObjects: ["256,192,2000,1,0,0:0:0:0:"],
                    mode: Beatmap.Mode.Mania
                )
            ),
        ]);

        var issues = context.RunBeatmapCheck<CheckUnusedLines>("Test");

        Assert.Empty(issues);
    }

    private static string BuildOsu(
        IEnumerable<string>? timingPoints = null,
        IEnumerable<string>? hitObjects = null,
        Beatmap.Mode mode = Beatmap.Mode.Standard
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            $"Mode: {(int)mode}",
            "[Metadata]",
            "Title:Unused Lines Test",
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
        };

        lines.AddRange(timingPoints ?? ["0,500,4,2,0,100,1,0"]);
        lines.Add("[HitObjects]");
        lines.AddRange(hitObjects ?? ["256,192,1000,1,0,0:0:0:0:"]);

        return string.Join("\n", lines);
    }
}
