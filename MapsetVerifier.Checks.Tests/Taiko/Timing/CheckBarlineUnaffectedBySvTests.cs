using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Timing;

public class CheckBarlineUnaffectedBySvTests
{
    // Barlines every 2000 ms.
    private static List<Issue> Run(double lineOffset)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .TimingPoints(
                    TestTimingPoints.Uninherited(0, 500),
                    TestTimingPoints.Inherited(lineOffset, -50)
                )
                .WithDefaultHitObject()
        );

        return context.RunBeatmapCheck<CheckBarlineUnaffectedBySv>("Test");
    }

    [Theory]
    [InlineData(4000)]
    [InlineData(3990)]
    [InlineData(5000)]
    public void SvChange_OnOrAwayFromABarline_IsFine(double offset)
    {
        Assert.Empty(Run(offset));
    }

    [Fact]
    public void SvChange_RightAfterABarline_IsAWarning()
    {
        // The barline at 4000 is snapped 3 ms before the line which should have modified it.
        var issue = Assert.Single(Run(4003));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("3 ms before", issue.message);
    }
}
