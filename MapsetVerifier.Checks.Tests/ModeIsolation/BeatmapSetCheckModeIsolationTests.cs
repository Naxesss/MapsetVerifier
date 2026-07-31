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
                    timingPoints:
                    [
                        OsuBuilder.DefaultTimingPoint,
                        TestTimingPoints.Inherited(0, -100),
                    ]
                )
            ),
            (
                "standard.osu",
                BuildOsu(
                    Beatmap.Mode.Standard,
                    "Easy",
                    circleSize: 5,
                    timingPoints:
                    [
                        OsuBuilder.DefaultTimingPoint,
                        TestTimingPoints.Inherited(0, -25),
                    ]
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
                BuildOsu(Beatmap.Mode.Taiko, "Oni", timingPoints: [OsuBuilder.DefaultTimingPoint])
            ),
            (
                "standard.osu",
                BuildOsu(
                    Beatmap.Mode.Standard,
                    "Hard",
                    timingPoints:
                    [
                        OsuBuilder.DefaultTimingPoint,
                        TestTimingPoints.Uninherited(1000, 500, effects: 1),
                        TestTimingPoints.Uninherited(2000, 500),
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
                        TestHitObjects.Circle(1000),
                        TestHitObjects.Circle(1800),
                        TestHitObjects.Circle(2600),
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
    ) =>
        new OsuBuilder()
            .Mode(mode)
            .Title("Mode Isolation")
            .Artist("MapsetVerifier")
            .Version(version)
            .CircleSize(circleSize)
            .TimingPoints(timingPoints ?? [OsuBuilder.DefaultTimingPoint])
            .HitObjects(hitObjects ?? [OsuBuilder.DefaultHitObject])
            .Build();
}
