using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckStackRhythmSwitchingTests
{
    private const int Beat = 333;
    private const int Half = 167;
    private const int Quarter3 = 250;

    [Fact]
    public void SameRhythm_IsNotReported()
    {
        using var context = Create(Stacks([Half, Half, Half, Half]));

        Assert.Empty(Run(context));
    }

    [Fact]
    public void SwitchingOnce_IsNotReported()
    {
        using var context = Create(Stacks([Half, Half, Beat, Beat]));

        Assert.Empty(Run(context));
    }

    [Fact]
    public void SwitchingBackAndForth_IsReportedOnce()
    {
        using var context = Create(Stacks([Half, Beat, Half, Beat]));

        var issue = Assert.Single(Run(context));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("3 times", issue.message);
        Assert.Contains("00:01:000 (1,1,1,1,1,1,1,1)", issue.message);
    }

    [Fact]
    public void SwitchingFarApart_IsNotReported()
    {
        using var context = Create(Stacks([Half, Beat, Half, Beat], pause: 20 * Beat));

        Assert.Empty(Run(context));
    }

    [Fact]
    public void OtherRhythms_AreNotReported()
    {
        // 3/4 is too close to 1/2 to be mistaken for its 1/1.
        using var context = Create(Stacks([Half, Quarter3, Half, Quarter3]));

        Assert.Empty(Run(context));
    }

    [Fact]
    public void SameRhythmsWithoutStacks_AreNotReported()
    {
        using var context = Create(Stacks([Half, Beat, Half, Beat], offset: 40));

        Assert.Empty(Run(context));
    }

    [Fact]
    public void StacksOnSliderTails_AreReported()
    {
        // Half a beat long, ending 70 px further.
        string[] SliderStack(int time, int gap, int x) =>
            [
                TestHitObjects.Slider(time, curvePoints: $"{x}:262", length: 70, x: x, type: 6),
                TestHitObjects.Circle(time + Half + gap, x, 262),
            ];

        using var context = Create([
            .. SliderStack(1000, Half, 100),
            .. SliderStack(2000, Beat, 200),
            .. SliderStack(3000, Half, 300),
        ]);

        Assert.Contains("2 times", Assert.Single(Run(context)).message);
    }

    [Fact]
    public void UnknownLevel_IsNotReported()
    {
        using var context = Create(Stacks([Half, Beat, Half, Beat]), "Test");

        Assert.Empty(context.RunBeatmapCheck<CheckStackRhythmSwitching>("Test"));
    }

    private static List<Issue> Run(CheckTestContext context) =>
        context.RunBeatmapCheck<CheckStackRhythmSwitching>("Normal");

    /// <summary> Pairs of circles with the given gaps between the two, each pair in another place. </summary>
    private static List<string> Stacks(int[] gaps, int pause = 2 * Beat, int offset = 0)
    {
        var objects = new List<string>();
        var time = 1000;

        for (var i = 0; i < gaps.Length; i++)
        {
            var x = 50 + i * 100;

            // Each in a new combo, so the next pair is at the top.
            objects.Add(TestHitObjects.Circle(time, x).Replace(",1,0,", ",5,0,"));
            objects.Add(
                TestHitObjects.Circle(time + gaps[i], x + offset).Replace(",1,0,", ",5,0,")
            );

            time += gaps[i] + pause;
        }

        return objects;
    }

    private static CheckTestContext Create(
        IEnumerable<string> hitObjects,
        string version = "Normal"
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Version(version)
                .TimingPoints(TestTimingPoints.Uninherited(1000, 60000d / 180))
                .HitObjects(hitObjects)
        );
}
