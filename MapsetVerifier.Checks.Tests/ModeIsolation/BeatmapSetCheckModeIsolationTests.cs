using MapsetVerifier.Checks.AllModes.General.Files;
using MapsetVerifier.Checks.Mania.Timing;
using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Checks.Taiko.Compose;
using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.ModeIsolation;

public class BeatmapSetCheckModeIsolationTests
{
    [Fact]
    public void HitsoundDiff_FlagsTemplateNamesAcrossModes()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("mania.osu", BuildOsu(Beatmap.Mode.Mania, "Main")),
            ("standard.osu", BuildOsu(Beatmap.Mode.Standard, "my hitsound diff")),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckHitsoundDiff>();

        var issue = Assert.Single(issues);
        Assert.Equal(
            "my hitsound diff may be a hitsound difficulty. If it were the case, ensure it is deleted before nominating this beatmap.",
            issue.message
        );
    }

    [Fact]
    public void EasySliderVelocity_DoesNotInspectNonManiaBeatmaps()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "mania.osu",
                BuildOsu(
                    Beatmap.Mode.Mania,
                    "Easy",
                    circleSize: 4,
                    timingPoints: ["0,500,4,2,0,100,1,0", "0,-100,4,2,0,100,0,0"]
                )
            ),
            (
                "standard.osu",
                BuildOsu(
                    Beatmap.Mode.Standard,
                    "Easy",
                    circleSize: 5,
                    timingPoints: ["0,500,4,2,0,100,1,0", "0,-25,4,2,0,100,0,0"]
                )
            ),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckEasySliderVelocity>();

        Assert.Empty(issues);
    }

    [Fact]
    public void KiaiConsistency_DoesNotCompareAgainstNonTaikoBeatmaps()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "taiko.osu",
                BuildOsu(Beatmap.Mode.Taiko, "Oni", timingPoints: ["0,500,4,2,0,100,1,0"])
            ),
            (
                "standard.osu",
                BuildOsu(
                    Beatmap.Mode.Standard,
                    "Hard",
                    timingPoints:
                    [
                        "0,500,4,2,0,100,1,0",
                        "1000,500,4,2,0,100,1,1",
                        "2000,500,4,2,0,100,1,0",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckKiaiConsistency>();

        Assert.Empty(issues);
    }

    [Fact]
    public void PatternLength_DoesNotInspectNonTaikoBeatmaps()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "taiko.osu",
                BuildOsu(
                    Beatmap.Mode.Taiko,
                    "Oni",
                    hitObjects:
                    [
                        "256,192,1000,1,0,0:0:0:0:",
                        "256,192,1800,1,0,0:0:0:0:",
                        "256,192,2600,1,0,0:0:0:0:",
                    ]
                )
            ),
            (
                "standard.osu",
                BuildOsu(
                    Beatmap.Mode.Standard,
                    "Easy",
                    hitObjects:
                    [
                        "128,192,1000,1,0,0:0:0:0:",
                        "220,192,1100,1,0,0:0:0:0:",
                        "310,192,1200,1,0,0:0:0:0:",
                        "420,192,1300,1,0,0:0:0:0:",
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckPatternLength>();

        Assert.Empty(issues);
    }

    [Fact]
    public void CloseOverlap_DoesNotInspectNonStandardBeatmaps()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "standard.osu",
                BuildOsu(
                    Beatmap.Mode.Standard,
                    "Easy",
                    hitObjects: ["128,192,1000,1,0,0:0:0:0:", "384,192,1300,1,0,0:0:0:0:"]
                )
            ),
            (
                "mania.osu",
                BuildOsu(
                    Beatmap.Mode.Mania,
                    "Easy",
                    hitObjects: ["64,192,1000,1,0,0:0:0:0:", "448,192,1100,1,0,0:0:0:0:"]
                )
            ),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckCloseOverlap>();

        Assert.Empty(issues);
    }

    private static string BuildOsu(
        Beatmap.Mode mode,
        string version,
        float circleSize = 4,
        IEnumerable<string>? timingPoints = null,
        IEnumerable<string>? hitObjects = null
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            $"Mode: {(int)mode}",
            "[Metadata]",
            "Title:Mode Isolation",
            "Artist:MapsetVerifier",
            "Creator:Tests",
            $"Version:{version}",
            "[Difficulty]",
            $"CircleSize:{circleSize}",
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
