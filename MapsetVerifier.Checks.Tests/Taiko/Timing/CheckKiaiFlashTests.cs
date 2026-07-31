using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Timing;

public class CheckKiaiFlashTests
{
    // 150 BPM → 400 ms/beat; GetNormalizedMsPerBeat leaves this unchanged.
    // Warning ≤ ceil(400/3) = 134; Minor ≤ ceil(400/2) = 200.

    [Fact]
    public void FlagsWarningWithOneThirdSnapLabel()
    {
        // Kiai on at 1000, off at 1133 → gap 133 ≈ 1/3
        var issues = RunCheck(
            timingPoints:
            [
                "0,400,4,2,0,100,1,0",
                "1000,-100,4,2,0,100,0,1",
                "1133,-100,4,2,0,100,0,0",
            ]
        );

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("Kiai flash (1/3)", issue.message);
        Assert.Contains("00:01:000", issue.message);
    }

    [Fact]
    public void FlagsMinorWithOneHalfSnapLabel()
    {
        // Kiai on at 1000, off at 1200 → gap 200 = 1/2
        var issues = RunCheck(
            timingPoints:
            [
                "0,400,4,2,0,100,1,0",
                "1000,-100,4,2,0,100,0,1",
                "1200,-100,4,2,0,100,0,0",
            ]
        );

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("Kiai flash (1/2)", issue.message);
        Assert.Contains("00:01:000", issue.message);
    }

    [Fact]
    public void DoesNotFlagFlashLongerThanHalfBeat()
    {
        // gap 201 > 200 → not flagged
        var issues = RunCheck(
            timingPoints:
            [
                "0,400,4,2,0,100,1,0",
                "1000,-100,4,2,0,100,0,1",
                "1201,-100,4,2,0,100,0,0",
            ]
        );

        Assert.Empty(issues);
    }

    private static List<Issue> RunCheck(IEnumerable<string> timingPoints)
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildTaikoOsu("Test", timingPoints)),
        ]);

        return context.RunBeatmapCheck<CheckKiaiFlash>("Test");
    }

    private static string BuildTaikoOsu(string version, IEnumerable<string> timingPoints) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename:",
            "Mode: 1",
            "[Metadata]",
            "Title:Kiai Flash",
            "Artist:Tests",
            "Creator:Tests",
            $"Version:{version}",
            "[Difficulty]",
            "CircleSize:5",
            "HPDrainRate:5",
            "OverallDifficulty:5",
            "ApproachRate:5",
            "SliderMultiplier:1.4",
            "SliderTickRate:1",
            "[Events]",
            "[TimingPoints]",
            string.Join('\n', timingPoints),
            "[HitObjects]",
            "256,192,1000,1,0,0:0:0:0:"
        );
}
