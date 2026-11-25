using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Patterns;

public class OsuPatternsTests
{
    private const string Red = "0,500,4,2,0,100,1,0";

    private static Beatmap Map(string[] hitObjects, params string[] timing) =>
        new(
            new OsuBuilder()
                .TimingPoints(timing.Length > 0 ? timing : [Red])
                .HitObjects(hitObjects)
                .Build(),
            "song",
            $"{Guid.NewGuid():N}.osu"
        );

    private static string Circle(int time, int type = 1) => $"256,192,{time},{type},0,0:0:0:0:";

    // 56 px at 140 px per 500 ms is 200 ms, which counts as 1/2 like anything from 125 up to 250 ms.
    private static string Half(int time) => TestHitObjects.Slider(time, length: 56);

    private static int[] Counts(IEnumerable<OsuPatterns.Run> runs) =>
        runs.Select(run => run.Count).ToArray();

    [Fact]
    public void Combos_StartOverAtNewCombosAndSpinners()
    {
        Assert.Equal(
            [2, 1, 2],
            Counts(
                OsuPatterns.Combos(
                    Map([
                        Circle(0),
                        Circle(500),
                        Circle(1000, 5),
                        TestHitObjects.Spinner(1500, 2000),
                        TestHitObjects.Spinner(2500, 3000),
                        Circle(3500),
                        Circle(4000),
                    ])
                )
            )
        );
        Assert.Empty(OsuPatterns.Combos(Map([TestHitObjects.Spinner(1500, 2000)])));
    }

    [Fact]
    public void Streams_EndAtGapsAndSpinners()
    {
        Assert.Equal(
            [3, 2, 2],
            Counts(
                OsuPatterns.Streams(
                    Map([
                        Circle(0),
                        Circle(100),
                        Circle(200),
                        Circle(1000),
                        Circle(1100),
                        TestHitObjects.Spinner(1200, 2000),
                        TestHitObjects.Spinner(2100, 2200),
                        Circle(2500),
                        Circle(2600),
                        Circle(4000),
                    ])
                )
            )
        );
    }

    [Fact]
    public void HalfSliders_NeedHalfGaps()
    {
        Assert.Equal(
            [3],
            Counts(
                OsuPatterns.HalfSliderRuns(
                    Map([Half(0), Half(400), Half(800), Circle(1200), Half(1400), Half(3000)])
                )
            )
        );
    }

    [Fact]
    public void SectionsWithoutRest_EndAtWholeBeats()
    {
        Assert.Equal(
            [3, 2],
            Counts(
                OsuPatterns.SectionsWithoutRest(
                    Map([
                        Circle(0),
                        Circle(200),
                        Circle(400),
                        Circle(1500),
                        Circle(1700),
                        TestHitObjects.Spinner(1800, 2200),
                    ])
                )
            )
        );
    }

    [Fact]
    public void SvChanges_AreCountedPerMinuteBetweenSliders()
    {
        // Over 30 seconds, where the green line at 10 s doesn't change any slider.
        var map = Map(
            [Half(0), Half(5000), Half(15000), Half(20000), Circle(30000)],
            Red,
            "5000,-50,4,2,0,100,0,0",
            "10000,-100,4,2,0,100,0,0",
            "12000,-50,4,2,0,100,0,0",
            "20000,-100,4,2,0,100,0,0"
        );

        Assert.Equal(4, OsuPatterns.SvChangesPerMinute(map));
        Assert.Equal(0, OsuPatterns.SvChangesPerMinute(Map([Circle(0)])));
        Assert.Equal(0, OsuPatterns.SvChangesPerMinute(Map([Circle(0), Circle(0)])));
    }

    [Fact]
    public void ClickedHalves_AndHalfGaps_AreCountedByTheirGaps()
    {
        var map = Map([Circle(0), Circle(200), Circle(400), Half(1000), Half(1400), Circle(3000)]);

        Assert.Equal([2], Counts(OsuPatterns.ClickedHalfRuns(map)));
        Assert.Equal([2, 1], Counts(OsuPatterns.HalfGapChains(map)));
        Assert.Equal(
            (2, 2, 2),
            (
                OsuPatterns.Measure(map).ClickedHalfRun,
                OsuPatterns.Measure(map).HalfSliderRun,
                OsuPatterns.Measure(map).HalfGapChain
            )
        );
        // A spinner right after ends them.
        Assert.Equal(
            [1],
            Counts(
                OsuPatterns.ClickedHalfRuns(
                    Map([Circle(0), Circle(200), TestHitObjects.Spinner(400, 900)])
                )
            )
        );
        Assert.Equal(0, OsuPatterns.Measure(Map([])).LongestCombo);
    }

    [Theory]
    [InlineData(123, false, false)]
    [InlineData(124, true, false)]
    [InlineData(246, true, false)]
    [InlineData(247, false, true)]
    public void HalfAndRest_AtTheirLimits(double ms, bool half, bool rest)
    {
        Assert.Equal(half, OsuPatterns.IsHalf(ms));
        Assert.Equal(rest, OsuPatterns.IsRest(ms));
    }
}
