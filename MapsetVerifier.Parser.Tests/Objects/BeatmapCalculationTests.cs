using MapsetVerifier.Parser.Difficulty;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

/// <summary>Values worked out from the whole beatmap: combos, drain time, countdown and timing changes.</summary>
public class BeatmapCalculationTests
{
    private static readonly string[] ThreeColours =
    [
        "Combo1 : 255,0,0",
        "Combo2 : 0,255,0",
        "Combo3 : 0,0,255",
    ];

    [Fact]
    public void DrainTime_WithoutObjects_IsZero()
    {
        var beatmap = TestBeatmap.WithObjects();

        Assert.Equal(0, beatmap.GetDrainTime(Beatmap.Mode.Standard));
        Assert.Equal(0, beatmap.GetPlayTime());
    }

    [Fact]
    public void DrainTime_LeavesOutBreaks_FromObjectToObject()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                [TestBeatmap.Circle(1000), TestBeatmap.Circle(11000), TestBeatmap.Circle(21000)],
                events: ["2,1200,10300"]
            )
        );

        // There's no drain between the objects around a break, rather than between its own times.
        Assert.Equal(10000, beatmap.GetDrainTime(Beatmap.Mode.Standard));
        Assert.Equal(20000, beatmap.GetPlayTime());
    }

    [Fact]
    public void DrainTime_EndsWithTheLastSpinner()
    {
        var beatmap = TestBeatmap.WithObjects(
            TestBeatmap.Circle(1000),
            "256,192,5000,12,0,9000,0:0:0:0:"
        );

        Assert.Equal(8000, beatmap.GetDrainTime(Beatmap.Mode.Standard));
    }

    [Fact]
    public void DrainTime_Mania_EndsWithTheLatestHoldNote_EvenIfItStartsEarlier()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                ["64,192,1000,128,0,9000:0:0:0:0:", "192,192,2000,1,0,0:0:0:0:"],
                mode: 3
            )
        );

        Assert.Equal(8000, beatmap.GetDrainTime(Beatmap.Mode.Mania));
    }

    [Fact]
    public void BreakAfterTheLastObject_DoesNotAddDrainTime()
    {
        // Left over when the objects after it are removed by hand.
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                [TestBeatmap.Circle(101000), TestBeatmap.Circle(102000)],
                events: ["2,102200,110000"]
            )
        );

        Assert.True(beatmap.GetDrainTime(Beatmap.Mode.Standard) <= beatmap.GetPlayTime());
    }

    [Fact]
    public void ObjectDensity_IsObjectsPerMsOfDrain()
    {
        var beatmap = TestBeatmap.WithObjects(TestBeatmap.Circle(0), TestBeatmap.Circle(4000));

        Assert.Equal(2 / 4000d, beatmap.GetObjectDensity(), 10);
    }

    [Theory]
    [InlineData(1000, 1)]
    [InlineData(2000, 2)]
    [InlineData(3000, 1)] // New combo.
    [InlineData(4000, 2)]
    [InlineData(5000, 1)] // Spinner.
    [InlineData(7000, 1)] // After a spinner.
    [InlineData(8000, 2)]
    public void Combo_IsTheNumberShownOnTheObject(double time, int expected)
    {
        var beatmap = TestBeatmap.WithObjects(
            TestBeatmap.Circle(1000),
            TestBeatmap.Circle(2000),
            TestBeatmap.Circle(3000, type: 5),
            TestBeatmap.Circle(4000),
            "256,192,5000,8,0,6000,0:0:0:0:",
            TestBeatmap.Circle(7000),
            TestBeatmap.Circle(8000)
        );

        Assert.Equal(expected, beatmap.GetCombo(beatmap.GetHitObject(time)!));
    }

    [Theory]
    [InlineData(1000, 1)]
    [InlineData(2000, 1)]
    [InlineData(3000, 2)]
    [InlineData(4000, 0)] // Wraps around after the third colour.
    public void ComboColour_AdvancesWithNewCombos_AndWrapsAround(double time, int expected)
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                [
                    TestBeatmap.Circle(1000, type: 5),
                    TestBeatmap.Circle(2000),
                    TestBeatmap.Circle(3000, type: 5),
                    TestBeatmap.Circle(4000, type: 5),
                ],
                colours: ThreeColours
            )
        );

        Assert.Equal(expected, beatmap.GetComboColourIndex(time));
    }

    [Theory]
    // Bits 4 to 6 are a 3 bit number of colours to skip on top of the new combo.
    [InlineData(5 + 16, 2)]
    [InlineData(5 + 32, 0)]
    [InlineData(5 + 16 + 32, 1)]
    [InlineData(5 + 64, 2)]
    public void ComboColour_SkipsColours(int type, int expected)
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code([TestBeatmap.Circle(1000, type)], colours: ThreeColours)
        );

        Assert.Equal(expected, beatmap.GetComboColourIndex(1000));
    }

    [Fact]
    public void ComboColour_IgnoresSpinners()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                [
                    TestBeatmap.Circle(1000, type: 5),
                    "256,192,2000,12,0,3000,0:0:0:0:",
                    TestBeatmap.Circle(4000, type: 5),
                ],
                colours: ThreeColours
            )
        );

        Assert.Equal(1, beatmap.GetComboColourIndex(3000));
        Assert.Equal(2, beatmap.GetComboColourIndex(4000));
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void DisplayedComboColour_ShowsTheFirstAsTheLast(int index, int expected)
    {
        var beatmap = TestBeatmap.Parse(TestBeatmap.Code(colours: ThreeColours));

        Assert.Equal(expected, beatmap.AsDisplayedComboColourIndex(index));
    }

    [Theory]
    [InlineData("Sprite,Foreground,Centre,\"a.png\",320,240", true)]
    [InlineData("Animation,Foreground,Centre,\"a.png\",320,240,2,100,LoopForever", true)]
    [InlineData("0,0,\"bg.jpg\",0,0", false)]
    [InlineData("Sample,1000,0,\"a.wav\",60", false)]
    public void DifficultySpecificStoryboard(string line, bool expected)
    {
        var beatmap = TestBeatmap.Parse(TestBeatmap.Code(events: [line]));

        Assert.Equal(expected, beatmap.HasDifficultySpecificStoryboard());
    }

    /// <summary>
    ///     The countdown takes 6 beats, plus any offset, before the first object. With a beat at 0 and 500 ms
    ///     per beat, a first object at 3000 ms leaves exactly enough room at normal speed (1 per beat).
    /// </summary>
    [Theory]
    [InlineData(1, 0, 3000, 0)]
    [InlineData(1, 0, 2500, -1)]
    [InlineData(1, 2, 4000, 0)]
    public void NormalCountdown_StartsSixBeatsBeforeTheFirstObject(
        int countdown,
        int beatOffset,
        int firstObject,
        double expected
    ) => Assert.Equal(expected, CountdownStart(countdown, beatOffset, firstObject), 5);

    [Theory]
    [InlineData(0, 6000, 0)]
    [InlineData(0, 5500, -1)]
    [InlineData(1, 7000, 0)]
    public void HalfSpeedCountdown_NeedsTwiceTheBeats(
        int beatOffset,
        int firstObject,
        double expected
    ) => Assert.Equal(expected, CountdownStart(2, beatOffset, firstObject), 5);

    [Fact]
    public void DoubleSpeedCountdown_NeedsLessThanHalfTheBeats() =>
        Assert.InRange(CountdownStart(3, 0, 10000), 20 - 3, 20);

    private static double CountdownStart(int countdown, int beatOffset, int firstObject) =>
        TestBeatmap
            .Parse(
                TestBeatmap.Code(
                    [TestBeatmap.Circle(firstObject)],
                    general: $"Countdown: {countdown}\nCountdownOffset: {beatOffset}"
                )
            )
            .GetCountdownStartBeat();

    [Fact]
    public void CountdownStart_WithoutObjects_IsZero()
    {
        Assert.Equal(0, TestBeatmap.WithObjects().GetCountdownStartBeat());
    }

    [Fact]
    public void OsuFileName_LeavesOutCharactersFilesCannotHave()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                metadata: "Title:What?\nArtist:AC/DC\nCreator:a|b\nVersion:\"Easy\" <3: *!"
            )
        );

        Assert.Equal("ACDC - What (ab) [Easy 3 !].osu", beatmap.GetOsuFileName());
    }

    [Fact]
    public void SvChanges_AreLinesWithAnotherSpeedThanTheOneBefore()
    {
        var beatmap = TestBeatmap.WithTiming(
            "0,500,4,2,0,100,1,0",
            "1000,-100,4,2,0,100,0,0",
            "2000,-50,4,2,0,100,0,0",
            "3000,-50,4,2,0,100,0,0",
            "4000,-200,4,2,0,100,0,0"
        );

        Assert.Equal([2000, 4000], beatmap.GetSvChanges().Select(line => line.Offset));
    }

    [Fact]
    public void VolumeChanges_AreLinesWithAnotherVolumeThanTheOneBefore()
    {
        var beatmap = TestBeatmap.WithTiming(
            "0,500,4,2,0,100,1,0",
            "1000,-100,4,2,0,100,0,0",
            "2000,-100,4,2,0,60,0,0",
            "3000,-100,4,2,0,60,0,0"
        );

        Assert.Equal([2000], beatmap.GetVolumeChanges().Select(line => line.Offset));
    }

    [Fact]
    public void KiaiToggles_IncludeKiaiFromTheStart()
    {
        var beatmap = TestBeatmap.WithTiming(
            "0,500,4,2,0,100,1,1",
            "1000,-100,4,2,0,100,0,1",
            "2000,-100,4,2,0,100,0,0",
            "3000,-100,4,2,0,100,0,9" // Kiai with an omitted bar line.
        );

        Assert.Equal([0, 2000, 3000], beatmap.GetKiaiToggles().Select(line => line.Offset));
    }

    [Theory]
    [InlineData(1000, null)]
    [InlineData(1001, null)]
    [InlineData(1005, 5d)]
    [InlineData(995, -5d)]
    public void UnsnapIssue_FromTwoMsOn(double time, double? expected)
    {
        Assert.Equal(expected, TestBeatmap.WithObjects().GetUnsnapIssue(time));
    }

    [Theory]
    [InlineData("osu file format v14", 14)]
    [InlineData("osu file format v128", 128)]
    [InlineData("﻿osu file format v5\r", 5)]
    [InlineData("[General]", int.MaxValue)]
    [InlineData("osu file format vX", int.MaxValue)]
    public void FileFormatVersion(string firstLine, int expected)
    {
        Assert.Equal(expected, TestBeatmap.Parse(firstLine + "\n").FileFormatVersion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MaximumLegacyScore_GrowsWithTheCombo(int mode)
    {
        long Score(int count) =>
            LegacyScoreHelper.GetMaximumLegacyScore(
                TestBeatmap.Parse(
                    TestBeatmap.Code(
                        Enumerable
                            .Range(0, count)
                            .Select(i => TestBeatmap.Circle(1000 + i * 250, x: 100 + i % 200))
                            .ToArray(),
                        mode: mode
                    )
                )
            );

        // At least 300 per object, and with combo scoring it grows faster than the object count.
        Assert.True(Score(100) >= 300 * 100);
        Assert.True(Score(200) > 2 * Score(100));
    }

    [Fact]
    public void MaximumLegacyScore_DoesNotApplyToMania()
    {
        var beatmap = TestBeatmap.Parse(TestBeatmap.Code(["64,192,1000,1,0,0:0:0:0:"], mode: 3));

        Assert.Throws<ArgumentException>(() => LegacyScoreHelper.GetMaximumLegacyScore(beatmap));
    }
}
