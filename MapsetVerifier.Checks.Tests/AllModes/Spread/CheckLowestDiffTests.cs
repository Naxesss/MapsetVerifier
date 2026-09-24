using MapsetVerifier.Checks.AllModes.Spread;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Spread;

public class CheckLowestDiffTests
{
    private record Diff(
        string Version,
        int DrainSeconds,
        int BreakSeconds = 0,
        Beatmap.Mode Mode = Beatmap.Mode.Standard,
        float Keys = 4
    );

    /// <summary>
    ///     Builds a map with one circle per second for <paramref name="diff" />'s drain time, with an optional break
    ///     halfway through.
    /// </summary>
    private static (string, string) Build(Diff diff)
    {
        var breakAt = diff.DrainSeconds / 2 * 1000;
        var breakMs = diff.BreakSeconds * 1000;
        var hitObjects = Enumerable
            .Range(0, diff.DrainSeconds + 1)
            .Select(i => i * 1000)
            .Select(time => time > breakAt ? time + breakMs : time)
            .Select(time => TestHitObjects.Circle(time));

        var builder = new OsuBuilder()
            .Mode(diff.Mode)
            .Version(diff.Version)
            .CircleSize(diff.Keys)
            .WithDefaultTiming()
            .HitObjects(hitObjects);

        if (breakMs > 0)
            builder.Events($"2,{breakAt + 200},{breakAt + breakMs + 800}");

        return ($"{diff.Version}.osu", builder.Build());
    }

    private static List<Issue> Run(params Diff[] diffs)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            diffs.Select(Build),
            extraFiles: ["audio.mp3"]
        );

        return context
            .RunBeatmapSetCheck<CheckLowestDiff>()
            .Where(issue =>
                issue.beatmap != null && issue.AppliesToDifficulty(issue.beatmap.GetDifficulty())
            )
            .ToList();
    }

    [Fact]
    public void Standard_HardLowest_ShortDrain_IsProblem()
    {
        var issues = Run(new Diff("Hard", 140), new Diff("Insane", 140));

        Assert.Contains(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void Standard_HardLowest_LongEnoughDrain_NoIssues()
    {
        var issues = Run(new Diff("Hard", 160), new Diff("Insane", 160));

        Assert.Empty(issues);
    }

    [Fact]
    public void Standard_BreakTime_DoesNotCount()
    {
        var issues = Run(new Diff("Hard", 140, BreakSeconds: 20), new Diff("Insane", 160));

        Assert.Contains(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void Taiko_BreakTime_CombinesWithDrain()
    {
        var issues = Run(
            new Diff("Muzukashii", 140, BreakSeconds: 20, Mode: Beatmap.Mode.Taiko),
            new Diff("Oni", 160, Mode: Beatmap.Mode.Taiko)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void Taiko_HighestDifficulty_BreakTimeCappedAt30Seconds()
    {
        var issues = Run(
            new Diff("Muzukashii", 160, Mode: Beatmap.Mode.Taiko),
            new Diff("Oni", 110, BreakSeconds: 60, Mode: Beatmap.Mode.Taiko)
        );

        Assert.Contains(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void Taiko_LowerDifficulty_BreakTimeNotCapped()
    {
        var issues = Run(
            new Diff("Muzukashii", 110, BreakSeconds: 60, Mode: Beatmap.Mode.Taiko),
            new Diff("Oni", 160, Mode: Beatmap.Mode.Taiko)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void Catch_PlatterLowest_ShortDrain_FewDiffs_IsProblem()
    {
        var issues = Run(
            new Diff("Platter", 140, Mode: Beatmap.Mode.Catch),
            new Diff("Rain", 140, Mode: Beatmap.Mode.Catch),
            new Diff("Overdose", 140, Mode: Beatmap.Mode.Catch)
        );

        Assert.Contains(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void Catch_PlatterLowest_ShortDrain_EnoughDiffs_IsSpreadWarning()
    {
        var issues = Run(
            new Diff("Platter", 140, Mode: Beatmap.Mode.Catch),
            new Diff("Rain", 140, Mode: Beatmap.Mode.Catch),
            new Diff("Overdose", 140, Mode: Beatmap.Mode.Catch),
            new Diff("Deluge", 140, Mode: Beatmap.Mode.Catch)
        );

        Assert.NotEmpty(issues);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
    }

    [Fact]
    public void Mania_EachKeyModeCheckedSeparately()
    {
        var issues = Run(
            new Diff("Normal", 110, Mode: Beatmap.Mode.Mania, Keys: 4),
            new Diff("Hard", 110, Mode: Beatmap.Mode.Mania, Keys: 7)
        );

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Equal("Hard", issue.beatmap?.MetadataSettings.version);
    }
}
