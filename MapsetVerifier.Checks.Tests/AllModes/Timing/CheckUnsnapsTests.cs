using MapsetVerifier.Checks.AllModes.Timing;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckUnsnapsTests
{
    [Fact]
    public void DoesNotFlagSliderTailSnappedToUpcomingMisalignedRedLine()
    {
        // Regression for chouchou merged syrups. - Havfrue (pearto) [test]: slider tails snapped to an
        // upcoming misaligned red line can land fractionally before it due to pixel-length math.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Unsnap Test")
                .SliderMultiplier(1.7f)
                .TimingPoints("0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0")
                .HitObjects("256,192,0,6,0,L|300:192,1,59.16,2|2,0:0:0:0:")
        );

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void StillFlagsUnsnappedCircle()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Unsnap Test")
                .SliderMultiplier(1.7f)
                .TimingPoints("0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0")
                .HitObjects("256,192,127,1,0,0:0:0:0:")
        );

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.NotEmpty(issues);
    }

    [Fact]
    public void StillFlagsSliderTailWellBeforeUpcomingMisalignedRedLine()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Unsnap Test")
                .SliderMultiplier(1.7f)
                .TimingPoints("0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0")
                .HitObjects("256,192,0,6,0,L|300:192,1,50,2|2,0:0:0:0:")
        );

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.Contains(issues, issue => issue.message.Contains("Slider tail"));
    }
}
