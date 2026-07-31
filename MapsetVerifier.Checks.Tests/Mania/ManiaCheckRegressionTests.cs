using MapsetVerifier.Checks.Mania.Timing;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania;

public class ManiaCheckRegressionTests
{
    /// <summary>
    ///     Issue #101: normalization green on the same offset as its red line must not trigger
    ///     "Normalized Value Moved Problem".
    /// </summary>
    [Fact]
    public void VariableBpm_NoNormalizedValueMoved_WhenGreenStackedOnRed()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "mania.osu",
                ManiaOsu.Build(
                    "Hard",
                    timingPoints:
                    [
                        OsuBuilder.DefaultTimingPoint,
                        TestTimingPoints.Inherited(0, -100),
                    ]
                )
            ),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckVariableBpm>();

        Assert.DoesNotContain(
            issues,
            i =>
                i.message.Contains(
                    "Isn't on top of the previous uninherited timing line",
                    StringComparison.Ordinal
                )
        );
    }
}
