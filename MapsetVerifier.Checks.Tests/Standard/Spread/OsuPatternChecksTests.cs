using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

/// <summary> All maps here are 180 BPM, so 1/1 is 333 ms, 1/2 is 167 ms and 1/4 is 83 ms. </summary>
public class OsuPatternChecksTests
{
    private const int Beat = 333;
    private const int Half = 167;
    private const int Quarter = 83;

    [Fact]
    public void ThreeClickedHalfRhythms_AreAllowed()
    {
        // 4 circles 1/2 apart are 3 clicked 1/2 rhythms.
        using var context = Create(Circles(1000, Half, 4));

        Assert.DoesNotContain(Run<CheckNormalRhythmPatterns>(context), IsTemplate("Clicked"));
    }

    [Fact]
    public void FourClickedHalfRhythms_AreReported()
    {
        using var context = Create(Circles(1000, Half, 5));

        var issue = Assert.Single(Run<CheckNormalRhythmPatterns>(context), IsTemplate("Clicked"));
        Assert.Contains("4 clicked 1/2 rhythms in a row", issue.message);
        Assert.Equal(Issue.Level.Warning, issue.level);
    }

    [Fact]
    public void HalfRhythmAt120Bpm_CountsAs1_1()
    {
        // 1/2 at 120 BPM is 250 ms, which the ranking criteria treat like 1/1 at 180 BPM.
        using var context = Create(Circles(1000, 250, 8), bpm: 120);

        Assert.Empty(OsuPatterns.ClickedHalfRuns(context.GetBeatmap("Test")));
    }

    [Fact]
    public void FiveHalfSlidersInARow_AreReported()
    {
        // 1/2 long sliders (length 70 at 1.4x is half a beat) with 1/2 gaps: a new slider every beat.
        var sliders = Enumerable
            .Range(0, 5)
            .Select(i => TestHitObjects.Slider(1000 + i * Beat, length: 70));
        using var context = Create(sliders, sliderMultiplier: 1.4f);

        var runs = OsuPatterns.HalfSliderRuns(context.GetBeatmap("Test")).ToList();

        var run = Assert.Single(runs);
        Assert.Equal(5, run.Count);
        Assert.Contains(Run<CheckNormalRhythmPatterns>(context), IsTemplate("Sliders"));
    }

    [Fact]
    public void LongSectionWithoutRest_IsReported()
    {
        // 20 beats of 1/2 with no 1/1 gap.
        using var context = Create(Circles(1000, Half, 41));

        var issue = Assert.Single(Run<CheckNormalRhythmPatterns>(context), IsTemplate("Rest"));
        Assert.Contains("20 beats without a 1/1 or longer gap", issue.message);
    }

    [Fact]
    public void RestEveryTwoMeasures_IsNotReported()
    {
        var circles = Enumerable
            .Range(0, 6)
            .SelectMany(bar => Circles(1000 + bar * 8 * Beat, Beat, 7));
        using var context = Create(circles);

        Assert.DoesNotContain(Run<CheckNormalRhythmPatterns>(context), IsTemplate("Rest"));
    }

    [Fact]
    public void StreamOfFiveNotes_IsAllowed()
    {
        using var context = Create(Circles(1000, Quarter, 5));

        Assert.Empty(Run<CheckStreamLength>(context));
    }

    [Fact]
    public void StreamOfSixNotes_IsReported()
    {
        using var context = Create(Circles(1000, Quarter, 6));

        var issue = Assert.Single(Run<CheckStreamLength>(context));
        Assert.Contains("Stream of 6 notes", issue.message);
    }

    [Fact]
    public void ComboOf17Objects_IsReported()
    {
        using var context = Create(Circles(1000, Beat, 17));

        var issue = Assert.Single(Run<CheckComboLength>(context));
        Assert.Contains("Combo of 17 objects", issue.message);
    }

    [Fact]
    public void NewCombos_SplitCombos()
    {
        // Type 5 is a circle with a new combo.
        var objects = Circles(1000, Beat, 17)
            .Select((line, i) => i == 8 ? line.Replace(",1,0,", ",5,0,") : line);
        using var context = Create(objects);

        Assert.Equal(
            [8, 9],
            OsuPatterns.Combos(context.GetBeatmap("Test")).Select(run => run.Count)
        );
        Assert.Empty(Run<CheckComboLength>(context));
    }

    private static Predicate<Issue> IsTemplate(string key) =>
        issue =>
            issue.message.Contains(
                key switch
                {
                    "Clicked" => "clicked 1/2 rhythms",
                    "Sliders" => "1/2 sliders in a row",
                    "Chain" => "1/2 gaps in a row",
                    _ => "without a 1/1",
                }
            );

    private static List<Issue> Run<TCheck>(CheckTestContext context)
        where TCheck : BeatmapCheck, new() => context.RunBeatmapCheck<TCheck>("Test");

    private static IEnumerable<string> Circles(int start, int gap, int count) =>
        Enumerable.Range(0, count).Select(i => TestHitObjects.Circle(start + i * gap));

    private static CheckTestContext Create(
        IEnumerable<string> hitObjects,
        double bpm = 180,
        float sliderMultiplier = 1.4f
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .SliderMultiplier(sliderMultiplier)
                .TimingPoints(TestTimingPoints.Uninherited(0, 60000 / bpm))
                .HitObjects(hitObjects)
        );
}
