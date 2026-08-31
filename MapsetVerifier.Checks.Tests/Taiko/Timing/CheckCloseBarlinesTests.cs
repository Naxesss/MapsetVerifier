using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Timing;

public class CheckCloseBarlinesTests
{
    // 4/4 at 500ms/beat → metronome 2000ms, half 1000ms

    [Fact]
    public void FlagsProblemWhenRestAtOrBelowHalfMetronome()
    {
        // Last barline at 2000, next red at 2400 → rest = 400 ≈ 1/1
        var issues = RunCheck(timingPoints: ["0,500,4,2,0,100,1,0", "2400,500,4,2,0,100,1,0"]);

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("very close to the previous barline (1/1)", issue.message);
        Assert.Contains("00:02:400", issue.message);
    }

    [Fact]
    public void FlagsProblemWhenRestExactlyAtHalfMetronome()
    {
        // Last barline at 2000, next red at 3000 → rest = 1000 = 2/1
        var issues = RunCheck(timingPoints: ["0,500,4,2,0,100,1,0", "3000,500,4,2,0,100,1,0"]);

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("very close to the previous barline (2/1)", issue.message);
        Assert.Contains("00:03:000", issue.message);
    }

    [Fact]
    public void FlagsWarningWhenRestBetweenHalfAndFullMetronome()
    {
        // Last barline at 2000, next red at 3500 → rest = 1500 = 3/1
        var issues = RunCheck(timingPoints: ["0,500,4,2,0,100,1,0", "3500,500,4,2,0,100,1,0"]);

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("close to the previous barline (3/1)", issue.message);
        Assert.DoesNotContain("rounding error", issue.message);
        Assert.Contains("00:03:500", issue.message);
    }

    [Fact]
    public void FlagsProblemWithSubBeatSnapLabel()
    {
        // Last barline at 2000, next red at 2250 → rest = 250 = 1/2
        var issues = RunCheck(timingPoints: ["0,500,4,2,0,100,1,0", "2250,500,4,2,0,100,1,0"]);

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("very close to the previous barline (1/2)", issue.message);
    }

    [Fact]
    public void DoesNotFlagExactMeasureBoundary()
    {
        var issues = RunCheck(timingPoints: ["0,500,4,2,0,100,1,0", "4000,500,4,2,0,100,1,0"]);

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagWhenJustShyOfCompleteMeasureDueToBpmRounding()
    {
        // 1ms short of 4/4: last downbeat is a full measure away, not a close barline.
        var issues = RunCheck(timingPoints: ["0,500,4,2,0,100,1,0", "3999,500,4,2,0,100,1,0"]);

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagYobanashiDeceiveRedLineResets()
    {
        // 260 BPM stored as 230.769230769231; integer red lines land 0.15–0.85ms
        // short of a complete 4/4 measure (JIN - Yobanashi Deceive).
        var issues = RunCheck(
            timingPoints:
            [
                "212,230.769230769231,4,2,0,100,1,0",
                "86981,230.769230769231,4,2,0,100,1,0",
                "157134,461.538461538462,4,2,0,100,1,0",
                "158980,230.769230769231,4,2,0,100,1,0",
            ]
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void SkipsWhenNextRedOmitsBarline()
    {
        // Would be Problem (rest = 400) without omit; effects 8 = OmitBarLine
        var issues = RunCheck(timingPoints: ["0,500,4,2,0,100,1,0", "2400,500,4,2,0,100,1,8"]);

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsWarningForSubHalfMillisecondRest()
    {
        // rest = 0.2 → soft Warning (possible float / borderline visibility)
        var issues = RunCheck(timingPoints: ["0,500,4,2,0,100,1,0", "2000.2,500,4,2,0,100,1,0"]);

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("close to the previous barline (1/16)", issue.message);
        Assert.DoesNotContain("rounding error", issue.message);
    }

    [Fact]
    public void FlagsThreeFourMeterUsingItsMetronome()
    {
        // 3/4 at 500ms/beat → metronome 1500, half 750
        // Last barline at 1500, next red at 1900 → rest = 400 → Problem (1/1)
        var issues = RunCheck(timingPoints: ["0,500,3,2,0,100,1,0", "1900,500,3,2,0,100,1,0"]);

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("very close to the previous barline (1/1)", issue.message);
        Assert.Contains("00:01:900", issue.message);
    }

    private static List<Issue> RunCheck(IEnumerable<string> timingPoints)
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", BuildTaikoOsu("Test", timingPoints)),
        ]);

        return context.RunBeatmapCheck<CheckCloseBarlines>("Test");
    }

    private static string BuildTaikoOsu(string version, IEnumerable<string> timingPoints) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename:",
            "Mode: 1",
            "[Metadata]",
            "Title:Close Barlines",
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
