using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

/// <summary>Looking up what is in effect, before or after a point in time.</summary>
public class BeatmapLookupTests
{
    private static readonly string[] Timing =
    [
        "1000,500,4,2,0,100,1,0",
        "2000,-50,4,2,0,80,0,0",
        "3000,400,4,2,0,60,1,0",
    ];

    private static Beatmap CircleAndSpinner() =>
        TestBeatmap.WithObjects(TestBeatmap.Circle(1000), "256,192,2000,12,0,3000,0:0:0:0:");

    [Theory]
    [InlineData(1000, 1000)]
    [InlineData(1999, 1000)]
    [InlineData(2000, 2000)]
    [InlineData(2999, 2000)]
    [InlineData(3000, 3000)]
    [InlineData(90000, 3000)]
    // Before any line the first one is used, since the game extends it backwards.
    [InlineData(-500, 1000)]
    public void TimingLine_IsTheLastOneStartedByThatTime(double time, double expectedOffset)
    {
        var beatmap = TestBeatmap.WithTiming(Timing);

        Assert.Equal(expectedOffset, beatmap.GetTimingLine(time)!.Offset);
    }

    [Theory]
    [InlineData(1995, false, false, 1000)]
    [InlineData(1995, true, false, 2000)]
    [InlineData(1994, true, false, 1000)]
    // Slider ticks only have 2 ms of leniency.
    [InlineData(1997, true, true, 1000)]
    [InlineData(1998, true, true, 2000)]
    public void TimingLine_HitSoundLeniency(
        double time,
        bool leniency,
        bool tick,
        double expectedOffset
    )
    {
        var beatmap = TestBeatmap.WithTiming(Timing);

        Assert.Equal(expectedOffset, beatmap.GetTimingLine(time, leniency, tick)!.Offset);
    }

    [Fact]
    public void TimingLine_OfAType_SkipsTheOtherType()
    {
        var beatmap = TestBeatmap.WithTiming(Timing);

        Assert.Equal(1000, beatmap.GetTimingLine<UninheritedLine>(2500)!.Offset);
        Assert.Equal(2000, beatmap.GetTimingLine<InheritedLine>(5000)!.Offset);
    }

    [Fact]
    public void TimingLine_WithoutAny_IsNull()
    {
        var beatmap = TestBeatmap.Parse(TestBeatmap.Code(timingPoints: []));

        Assert.Null(beatmap.GetTimingLine(0));
        Assert.Null(beatmap.GetNextTimingLine(0));
        Assert.Null(beatmap.GetSliderVelocityLine(0));
    }

    [Fact]
    public void TimingLines_AreSorted_WithRedLinesFirstOnTheSameOffset()
    {
        var beatmap = TestBeatmap.WithTiming(
            "3000,-100,4,2,0,100,0,0",
            "1000,-50,4,2,0,100,0,0",
            "1000,500,4,2,0,100,1,0"
        );

        Assert.Equal([1000, 1000, 3000], beatmap.TimingLines.Select(line => line.Offset));
        Assert.IsType<UninheritedLine>(beatmap.TimingLines[0]);
        Assert.IsType<InheritedLine>(beatmap.TimingLines[1]);
    }

    [Theory]
    [InlineData(1000, 2000d)]
    [InlineData(2500, 3000d)]
    [InlineData(3000, null)]
    public void NextTimingLine_IsTheFirstStartingAfter(double time, double? expectedOffset)
    {
        var beatmap = TestBeatmap.WithTiming(Timing);

        Assert.Equal(expectedOffset, beatmap.GetNextTimingLine(time)?.Offset);
    }

    [Fact]
    public void NextTimingLine_BeforeTheFirst_IsTheFirst()
    {
        var beatmap = TestBeatmap.WithTiming(Timing);

        Assert.Equal(1000, beatmap.GetNextTimingLine(0)!.Offset);
    }

    [Fact]
    public void NextHitObject_BeforeTheFirst_IsTheFirst()
    {
        var beatmap = TestBeatmap.WithObjects(TestBeatmap.Circle(1000), TestBeatmap.Circle(2000));

        Assert.Equal(1000, beatmap.GetNextHitObject(0)!.time);
    }

    [Theory]
    [InlineData(1000, 2000d)]
    [InlineData(1500, 2000d)]
    [InlineData(2000, null)]
    public void NextHitObject_IsTheFirstStartingAfter(double time, double? expected)
    {
        var beatmap = TestBeatmap.WithObjects(TestBeatmap.Circle(1000), TestBeatmap.Circle(2000));

        Assert.Equal(expected, beatmap.GetNextHitObject(time)?.time);
    }

    [Theory]
    [InlineData(500, null)]
    [InlineData(1000, null)]
    [InlineData(1500, 1000d)]
    // Still inside the spinner from 2000 to 3000, so that isn't a previous object yet.
    [InlineData(2500, 1000d)]
    [InlineData(3001, 2000d)]
    public void PrevHitObject_IsTheLastOneEndedByThen(double time, double? expected)
    {
        Assert.Equal(expected, CircleAndSpinner().GetPrevHitObject(time)?.time);
    }

    [Fact]
    public void HitObjectLookups_OfAType_SkipOtherTypes()
    {
        var beatmap = TestBeatmap.WithObjects(
            TestBeatmap.Circle(1000),
            "256,192,2000,12,0,3000,0:0:0:0:",
            TestBeatmap.Circle(4000)
        );

        Assert.Equal(1000, beatmap.GetHitObject<Circle>(3500)!.time);
        Assert.Equal(4000, beatmap.GetNextHitObject<Circle>(1500)!.time);
        Assert.Equal(1000, beatmap.GetPrevHitObject<Circle>(3500)!.time);
        Assert.Null(beatmap.GetHitObject<Slider>(3500));
        Assert.Null(beatmap.GetNextHitObject<Slider>(0));
        Assert.Null(beatmap.GetPrevHitObject<Slider>(9000));
    }

    [Theory]
    [InlineData(0, 1000, false)] // The end is exclusive,
    [InlineData(1000, 1001, true)] // and the start inclusive.
    [InlineData(1001, 2000, false)]
    [InlineData(1500, 2500, true)]
    [InlineData(2500, 9000, false)] // The spinner is still going, but doesn't start here.
    public void HasHitObjectStartingBetween(double start, double end, bool expected)
    {
        Assert.Equal(
            expected,
            CircleAndSpinner().HasHitObjectStartingBetween<HitObject>(start, end)
        );
    }

    [Theory]
    [InlineData(0, 1000, false)]
    [InlineData(1001, 2000, false)]
    [InlineData(2500, 2600, true)]
    [InlineData(3000, 4000, true)] // Ending exactly at the start counts.
    [InlineData(3001, 4000, false)]
    public void HasHitObjectOverlapping(double start, double end, bool expected)
    {
        Assert.Equal(expected, CircleAndSpinner().HasHitObjectOverlapping<HitObject>(start, end));
    }

    [Fact]
    public void HasHitObjectOverlapping_FindsALongObjectBehindShorterOnes()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                ["64,192,1000,128,0,9000:0:0:0:0:", "192,192,2000,1,0,0:0:0:0:"],
                mode: 3
            )
        );

        Assert.True(beatmap.HasHitObjectOverlapping<HitObject>(5000, 6000));
        Assert.False(beatmap.HasHitObjectOverlapping<Circle>(5000, 6000));
    }

    [Fact]
    public void HitObjects_AreSortedByTime_AndKnowTheirNeighbours()
    {
        var beatmap = TestBeatmap.WithObjects(
            TestBeatmap.Circle(3000),
            TestBeatmap.Circle(1000),
            TestBeatmap.Circle(2000)
        );
        var middle = beatmap.HitObjects[1];

        Assert.Equal(2000, middle.time);
        Assert.Equal(1000, middle.Prev()!.time);
        Assert.Equal(3000, middle.Next()!.time);
        Assert.Null(beatmap.HitObjects[0].Prev());
        Assert.Null(beatmap.HitObjects[2].Next());
    }

    [Fact]
    public void SkippingConcurrentObjects_FindsTheNextDistinctTime()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                [
                    "64,192,1000,1,0,0:0:0:0:",
                    "192,192,1000,1,0,0:0:0:0:",
                    "320,192,1000,1,0,0:0:0:0:",
                    "64,192,2000,1,0,0:0:0:0:",
                ],
                mode: 3
            )
        );

        Assert.Equal(2000, beatmap.HitObjects[0].Next(true)!.time);
        Assert.Equal(1000, beatmap.HitObjects[0].Next()!.time);
        Assert.Equal(1000, beatmap.HitObjects[3].Prev(true)!.time);
    }

    [Fact]
    public void SkippingConcurrentObjects_WithNothingElse_IsNull()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(["64,192,1000,1,0,0:0:0:0:", "192,192,1000,1,0,0:0:0:0:"], mode: 3)
        );

        Assert.Null(beatmap.HitObjects[0].Next(true));
        Assert.Null(beatmap.HitObjects[1].Prev(true));
    }

    [Fact]
    public void SkippingConcurrentLines_FindsTheNextDistinctOffset()
    {
        var beatmap = TestBeatmap.WithTiming(
            "1000,500,4,2,0,100,1,0",
            "1000,-50,4,2,0,100,0,0",
            "2000,-100,4,2,0,100,0,0"
        );

        Assert.Equal(2000, beatmap.TimingLines[0].Next(true)!.Offset);
        Assert.Same(beatmap.TimingLines[1], beatmap.TimingLines[0].Next());
        Assert.Same(beatmap.TimingLines[1], beatmap.TimingLines[2].Prev(true));
        Assert.Null(beatmap.TimingLines[2].Next());
        Assert.Null(beatmap.TimingLines[0].Prev());
    }

    [Fact]
    public void SkippingConcurrentLines_WithNothingElse_IsNull()
    {
        var beatmap = TestBeatmap.WithTiming("1000,500,4,2,0,100,1,0", "1000,-50,4,2,0,100,0,0");

        Assert.Null(beatmap.TimingLines[0].Next(true));
        Assert.Null(beatmap.TimingLines[1].Prev(true));
    }

    [Fact]
    public void SliderVelocityLine_CountsDecimalOffsetsAsWholeMilliseconds()
    {
        var beatmap = TestBeatmap.WithTiming("0,500,4,2,0,100,1,0", "1000.9,-50,4,2,0,100,0,0");

        Assert.Equal(1, beatmap.GetSliderVelocityLine(999)!.SvMult);
        Assert.Equal(2, beatmap.GetSliderVelocityLine(1000)!.SvMult);
        Assert.Equal(1, beatmap.GetTimingLine(1000)!.SvMult);
    }

    [Fact]
    public void ClearCacheForSongPath_LetsAReloadSeeFreshData()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var before = TestBeatmap.Code([TestBeatmap.Circle(1000)]);
        var after = TestBeatmap.Code([TestBeatmap.Circle(5000)]);

        Assert.Equal(1000, new Beatmap(before, folder, "a.osu").GetHitObject(0)!.time);
        Beatmap.ClearCacheForSongPath(folder);

        var reloaded = new Beatmap(after, folder, "a.osu");
        Assert.Equal(5000, reloaded.GetHitObject(0)!.time);
        Assert.False(reloaded.HasHitObjectOverlapping<HitObject>(0, 2000));
    }

    [Fact]
    public void OverlappingObjects_NeedOneOfTheType()
    {
        var beatmap = TestBeatmap.WithObjects(
            TestBeatmap.Circle(1000),
            "256,192,3000,12,0,4000,0:0:0:0:"
        );

        Assert.False(beatmap.HasHitObjectOverlapping<Slider>(0, 10000));
        Assert.True(beatmap.HasHitObjectOverlapping<Spinner>(3500, 3600));
        Assert.True(beatmap.HasHitObjectOverlapping<Spinner>(4000, 5000));
        Assert.False(beatmap.HasHitObjectOverlapping<Spinner>(4001, 5000));
        Assert.False(beatmap.HasHitObjectOverlapping<Spinner>(2000, 3000));
    }
}
