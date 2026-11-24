using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckKiaiUnsnapTests
{
    private static List<Issue> Run(Beatmap.Mode mode, params string[] timingPoints)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(mode)
                .TimingPoints(TestTimingPoints.Uninherited(0, 500))
                .TimingPoints(timingPoints)
                .WithDefaultHitObject()
        );

        return context.RunBeatmapCheck<CheckKiaiUnsnap>("Test");
    }

    private static string Kiai(int offset) => TestTimingPoints.Inherited(offset, -100, effects: 1);

    private static string NoKiai(int offset) => TestTimingPoints.Inherited(offset, -100);

    [Theory]
    [InlineData(Beatmap.Mode.Standard, 2000, null)]
    [InlineData(Beatmap.Mode.Standard, 2001, Issue.Level.Minor)]
    [InlineData(Beatmap.Mode.Standard, 2009, Issue.Level.Minor)]
    [InlineData(Beatmap.Mode.Standard, 2010, Issue.Level.Warning)]
    [InlineData(Beatmap.Mode.Taiko, 2004, Issue.Level.Minor)]
    [InlineData(Beatmap.Mode.Taiko, 2005, Issue.Level.Warning)]
    public void Start(Beatmap.Mode mode, int offset, Issue.Level? expected)
    {
        var issues = Run(mode, Kiai(offset), NoKiai(4000));

        Assert.Equal(expected, issues.SingleOrDefault()?.level);
    }

    [Fact]
    public void End_UnsnappedIsMinor()
    {
        var issue = Assert.Single(Run(Beatmap.Mode.Standard, Kiai(2000), NoKiai(4012)));

        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("Kiai end is unsnapped by 12 ms", issue.message);
    }

    [Fact]
    public void LinesWithinKiai_DoNotStartIt()
    {
        Assert.Empty(Run(Beatmap.Mode.Standard, Kiai(2000), Kiai(2512), NoKiai(4000)));
    }

    [Fact]
    public void End_IsAlsoChecked_WithLinesWithinKiai()
    {
        var issue = Assert.Single(Run(Beatmap.Mode.Standard, Kiai(2000), Kiai(3000), NoKiai(4012)));

        Assert.Contains("Kiai end", issue.message);
    }

    [Fact]
    public void KiaiFromTheFirstLine_HasNoStartToSnap()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .TimingPoints("0,500,4,2,0,100,1,1", "0,-100,4,2,0,100,0,1")
                .WithDefaultHitObject()
        );

        Assert.Empty(context.RunBeatmapCheck<CheckKiaiUnsnap>("Test"));
    }
}
