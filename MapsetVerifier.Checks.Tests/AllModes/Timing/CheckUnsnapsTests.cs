using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Parser.Objects;
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

    [Fact]
    public void DoesNotFlagSliderTailUnderSvBelowTheEditorMinimum()
    {
        // Regression for pa-o-mu99999 - Bobobo-bo Bo-bobo (Jayceko) [!!!   !!]: the drumroll under the
        // 0.0932x line ends at exactly 77416.25 (a 1/4 snap). Clamping sv to 0.1x shortened it to
        // 77397 and reported a unsnap.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .Title("Unsnap Test")
                .SliderMultiplier(1.4f)
                .TimingPoints("1010,375,4,1,0,100,1,0", "77115,-1072.64774244139,4,1,0,100,0,1")
                .HitObjects("279,82,77135,2,4,L|271:77,1,9.78886166473012")
        );

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagSliderTailLandingExactlyOnAWholeMs()
    {
        // Regression for STEREO DIVE FOUNDATION - PEACEKEEPER (TV Size) [Weeder's Expert]: the tail lands on
        // 47675, an exactly representable grid point, but the float pixel length puts it 4 ns late. Deriving the
        // snapped time as `time - theoreticalUnsnap` cancelled that down to 47674.999999999993, which truncated
        // to 47674 and reported a 1 ms unsnap on a perfectly snapped tail.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Unsnap Test")
                .SliderMultiplier(1.6f)
                .TimingPoints("8900,300,4,2,1,60,1,0", "47300,-133.333333333333,4,2,1,60,0,0")
                .HitObjects("419,134,47600,2,0,P|434:128|447:125,1,30.0000011444092")
        );

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void StillFlagsObjectTruncatedOffAWholeMsGridPoint()
    {
        // The counterpart to the above: 1 ms unsnaps are only so common because the editor truncates, so an
        // object sitting a whole ms before an exact grid point still has to be reported.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Unsnap Test")
                .SliderMultiplier(1.6f)
                .TimingPoints("8900,300,4,2,1,60,1,0")
                .HitObjects("256,192,47674,1,0,0:0:0:0:")
        );

        var issues = context.RunBeatmapCheck<CheckUnsnaps>("Test");

        Assert.Contains(issues, issue => issue.message.Contains("unsnapped by -1 ms"));
    }
}
