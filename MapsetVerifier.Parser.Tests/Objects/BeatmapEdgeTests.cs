using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Objects.HitObjects;
using Xunit;
using static MapsetVerifier.Parser.Objects.HitSample;

namespace MapsetVerifier.Parser.Tests.Objects;

public class BeatmapEdgeTests
{
    private const string GreenLine = "0,-100,4,2,0,100,0,0";

    private static Beatmap OnlyGreenLines() =>
        TestBeatmap.Parse(TestBeatmap.Code([TestBeatmap.Circle(1000)], timingPoints: [GreenLine]));

    [Fact]
    public void WithoutRedLines_SnappingCannotBeToldAndSaysSo()
    {
        var beatmap = OnlyGreenLines();

        Assert.Contains(
            "No uninherited line",
            Assert.ThrowsAny<Exception>(() => beatmap.GetOffsetIntoBeat(1000)).Message
        );
        Assert.ThrowsAny<Exception>(() => beatmap.GetTheoreticalUnsnap(1000, 4));
        Assert.ThrowsAny<Exception>(() => beatmap.GetPracticalUnsnap(1000, 4));
        Assert.Equal(0, beatmap.GetCountdownStartBeat());
    }

    [Fact]
    public void SliderVelocityLine_BeforeTheFirst_IsTheFirst()
    {
        var beatmap = TestBeatmap.WithTiming("1000,500,4,2,0,100,1,0");

        Assert.Equal(1000, beatmap.GetSliderVelocityLine(0)!.Offset);
    }

    [Fact]
    public void ClearingTheCacheOfOtherFolders_KeepsThisOne()
    {
        var beatmap = TestBeatmap.WithObjects(TestBeatmap.Circle(1000));
        Assert.Equal(1000, beatmap.GetHitObject(1000)!.time);

        Beatmap.ClearCacheForSongPath("another" + Path.DirectorySeparatorChar);

        Assert.Equal(1000, beatmap.GetHitObject(1000)!.time);
    }

    [Fact]
    public void TimedAttributes_AreOnlyWorkedOutOnce()
    {
        var beatmap = TestBeatmap.WithObjects(
            TestBeatmap.Circle(1000),
            TestBeatmap.Circle(1500, x: 100)
        );

        Assert.Same(
            beatmap.EnsureTimedAttributesCalculated(),
            beatmap.EnsureTimedAttributesCalculated()
        );
    }

    [Theory]
    [InlineData(Beatmap.Difficulty.Easy, "an Easy", "Kantan", "Cup", "Easy")]
    [InlineData(Beatmap.Difficulty.Normal, "a Normal", "Futsuu", "Salad", "Normal")]
    [InlineData(Beatmap.Difficulty.Hard, "a Hard", "Muzukashii", "Platter", "Hard")]
    [InlineData(Beatmap.Difficulty.Insane, "an Insane", "Oni", "Rain", "Insane")]
    [InlineData(Beatmap.Difficulty.Expert, "an Expert", "Inner Oni", "Overdose", "Expert")]
    [InlineData(Beatmap.Difficulty.Ultra, "an Ultra", "Inner Oni", "Overdose", "Expert")]
    public void DifficultyNames(
        Beatmap.Difficulty difficulty,
        string general,
        string taiko,
        string fruits,
        string mania
    )
    {
        string Name(int mode) =>
            TestBeatmap.Parse(TestBeatmap.Code(mode: mode)).GetModeDifficultyName(difficulty);

        Assert.Equal(general, TestBeatmap.WithObjects().GetDifficultyName(difficulty));
        Assert.Equal(taiko, Name(1));
        Assert.Equal(fruits, Name(2));
        Assert.Equal(mania, Name(3));
    }

    [Fact]
    public void DifficultyNames_FollowTheMapItself()
    {
        var beatmap = TestBeatmap.Parse(TestBeatmap.Code(metadata: "Version:Hard"));

        Assert.Equal("a Hard", beatmap.GetDifficultyName());
        Assert.Equal("Hard", beatmap.GetModeDifficultyName());
    }

    [Fact]
    public void AudioFile_WithoutAName_IsNone()
    {
        Assert.Null(TestBeatmap.WithObjects().GetAudioFilePath());
    }

    [Fact]
    public void BackgroundOfAMapWithoutAFolder_IsNone()
    {
        var beatmap = new Beatmap(
            TestBeatmap.Code(events: ["0,0,\"bg.jpg\",0,0"]),
            "",
            $"{Guid.NewGuid():N}.osu"
        );

        Assert.Null(beatmap.GetBackgroundFilePath());
    }

    [Fact]
    public void BreakOfAMapWithoutObjects_HasNoDuration()
    {
        var beatmap = TestBeatmap.Parse(TestBeatmap.Code(events: ["2,1000,5000"]));

        Assert.Equal(0, beatmap.Breaks[0].GetDuration(beatmap));
    }

    [Fact]
    public void LoopWithoutCommands_TakesNoTime()
    {
        var loop = new StoryboardLoop(1000, 5);

        Assert.Equal(1000, loop.GetStartTime());
        Assert.Equal(1000, loop.GetEndTime());
    }

    [Fact]
    public void Sounds_OfEveryKindOfObject()
    {
        var beatmap = TestBeatmap.WithObjects(
            TestBeatmap.Circle(1000, hitSound: 2),
            "256,192,2000,12,4,3000,1:2:0:0:",
            "0,100,4000,2,8,L|280:100,2,140,2|4|8,1:2|3:1|2:3,0:0:0:0:"
        );
        var (circle, spinner, slider) = (
            beatmap.HitObjects[0],
            beatmap.HitObjects[1],
            beatmap.HitObjects[2]
        );

        Assert.Equal(HitObject.HitSounds.Whistle, circle.GetStartHitSound());
        Assert.Null(circle.GetEndHitSound());
        Assert.Null(circle.GetEndSampleset());
        Assert.Null(circle.GetSliderSlide());
        Assert.Equal(SamplesetType.Soft, circle.GetStartSampleset());

        Assert.Null(spinner.GetStartHitSound());
        Assert.Null(spinner.GetStartSampleset());
        Assert.Equal(HitObject.HitSounds.Finish, spinner.GetEndHitSound());
        Assert.Equal(SamplesetType.Normal, spinner.GetEndSampleset());
        Assert.Equal(SamplesetType.Soft, spinner.GetEndSampleset(true));

        Assert.Equal(HitObject.HitSounds.Whistle, slider.GetStartHitSound());
        Assert.Equal(HitObject.HitSounds.Clap, slider.GetEndHitSound());
        Assert.Equal(HitObject.HitSounds.Clap, slider.GetSliderSlide());
        Assert.Equal(SamplesetType.Normal, slider.GetStartSampleset());
        Assert.Equal(SamplesetType.Soft, slider.GetStartSampleset(true));
        Assert.Equal(SamplesetType.Soft, slider.GetEndSampleset());
        Assert.Equal(SamplesetType.Drum, slider.GetEndSampleset(true));

        var reverse = (Slider)slider;
        Assert.Equal(SamplesetType.Drum, reverse.GetReverseSampleset(0));
        Assert.Equal(SamplesetType.Normal, reverse.GetReverseSampleset(0, true));
        // Past the last one, so it plays what the line says.
        Assert.Equal(SamplesetType.Soft, reverse.GetReverseSampleset(5, true));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("custom", true)]
    [InlineData("custom.wav", true)]
    public void OwnHitSoundFile_ReplacesEverythingElse(string file, bool used)
    {
        var circle = TestBeatmap
            .Parse(
                TestBeatmap.Code(
                    ["256,192,1000,1,2,0:0:0:0:" + file],
                    timingPoints: ["0,500,4,2,1,100,1,0"]
                )
            )
            .HitObjects[0];

        var names = circle.GetUsedHitSoundFileNames().ToList();

        Assert.Equal(used, names.Count > 0);

        if (file.Length > 0)
            Assert.Equal(["custom"], names);
        else
            Assert.Contains("soft-hitwhistle", names);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    public void Ticks_SoundInStandardAndCatchOnly(int mode, bool expected)
    {
        var slider = TestBeatmap
            .Parse(
                TestBeatmap.Code(
                    ["0,100,1000,2,2,L|280:100,1,280,0|0,0:0|0:0,0:3:0:0:"],
                    mode: mode
                )
            )
            .HitObjects[0];

        Assert.Equal(expected, slider.usedHitSamples.Any(s => s.HitSource == HitSourceType.Tick));
        Assert.Equal(mode == 0, slider.usedHitSamples.Any(s => s.HitSource == HitSourceType.Body));

        // The whistle of the slide plays the addition, the slide itself the line's.
        if (mode == 0)
            Assert.Equal(
                [SamplesetType.Soft, SamplesetType.Drum],
                slider
                    .usedHitSamples.Where(s => s.HitSource == HitSourceType.Body)
                    .Select(s => s.Sampleset!.Value)
                    .Distinct()
            );
    }

    [Theory]
    [InlineData("B4|100:100|200:100|300:100", Slider.Curve.BSpline)]
    [InlineData("B0|100:100|200:100|300:100", Slider.Curve.Catmull)]
    [InlineData("Bx|100:100|200:100|300:100", Slider.Curve.Catmull)]
    [InlineData("B|100:100|200:100|300:100", Slider.Curve.Bezier)]
    [InlineData("P|200:150|300:100", Slider.Curve.Passthrough)]
    [InlineData("C|100:150|200:100|300:150", Slider.Curve.Catmull)]
    [InlineData("C|300:100", Slider.Curve.Catmull)]
    [InlineData("L|300:100", Slider.Curve.Linear)]
    public void CurveTypes_AllHaveAPath(string curve, Slider.Curve expected)
    {
        var slider = (Slider)TestBeatmap.WithObjects($"0,100,1000,2,0,{curve},1,140").HitObjects[0];

        Assert.Equal(expected, slider.CurveType);
        Assert.NotEqual(slider.Position, slider.EndPosition);
        Assert.NotEqual(slider.Position, slider.GetPathPosition(1250));
    }

    [Fact]
    public void EdgeSoundsWithoutSeparator_AreNone()
    {
        var slider = (Slider)
            TestBeatmap.WithObjects("0,100,1000,2,0,L|280:100,1,140,2").HitObjects[0];

        Assert.Equal(HitObject.HitSounds.None, slider.StartHitSound);
    }

    [Fact]
    public void WithoutTimingLines_LookupsAreEmpty()
    {
        var beatmap = TestBeatmap.Parse(TestBeatmap.Code(timingPoints: []));

        Assert.Null(beatmap.GetSliderVelocityLine(1000));
        Assert.Null(beatmap.GetTimingLine(1000));
        Assert.Null(beatmap.GetNextTimingLine(1000));
    }

    [Fact]
    public void SliderWithoutRedLines_SaysWhatIsMissing()
    {
        Assert.Contains(
            "No uninherited",
            Assert
                .ThrowsAny<Exception>(() =>
                    TestBeatmap.Parse(
                        TestBeatmap.Code(
                            ["0,100,1000,2,0,L|280:100,1,140"],
                            timingPoints: [GreenLine]
                        )
                    )
                )
                .Message
        );
    }

    [Theory]
    [InlineData("0,500,4,2,0,100,1,0", 4000, 2)]
    [InlineData("5,500,4,2,0,100,1,0", 4005, 2)]
    [InlineData("1006,500,4,2,0,100,1,0", 4006, 3)]
    [InlineData("1000,500,4,2,0,100,1,0", 4000, 2)]
    public void Countdown_LosesABeat_IfTheFirstIsCutOff(string line, int time, double expected)
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                [TestBeatmap.Circle(time)],
                timingPoints: [line],
                general: "Countdown: 1"
            )
        );

        Assert.Equal(expected, beatmap.GetCountdownStartBeat());
    }

    [Theory]
    [InlineData(1500, 60, "Easy")]
    [InlineData(80, 500, "Ultra")]
    public void NamelessDifficulty_IsToldByItsStars(int interval, int distance, string expected)
    {
        var beatmap = TestBeatmap.WithObjects([
            .. Enumerable
                .Range(0, 80)
                .Select(i => TestBeatmap.Circle(1000 + i * interval, x: i % 2 * distance)),
        ]);

        Assert.Equal(expected, beatmap.GetDifficulty().ToString());
    }

    [Fact]
    public void CirclesUnderASliderTail_StackDownwards()
    {
        var beatmap = TestBeatmap.WithObjects(
            "100,100,1000,2,0,L|240:100,1,140",
            TestBeatmap.Circle(1600, x: 240, y: 100),
            TestBeatmap.Circle(1700, x: 240, y: 100),
            TestBeatmap.Circle(1800, x: 240, y: 100)
        );

        Assert.Equal([0, -1, -2, -3], beatmap.HitObjects.Select(o => ((Stackable)o).stackIndex));
    }

    [Fact]
    public void LinearSlider_WithManyNodes_FollowsAllOfThem()
    {
        var nodes = string.Join("|", Enumerable.Range(1, 600).Select(i => $"{i % 2}:{i / 2}"));
        var slider = (Slider)TestBeatmap.WithObjects($"0,0,1000,2,0,L|{nodes},1,600").HitObjects[0];

        Assert.True(slider.GetPathPosition(slider.time + slider.GetCurveDuration() / 2).Y > 50);
    }

    [Fact]
    public void LongSlider_SoundsOnABeatLimit()
    {
        var slider = TestBeatmap
            .Parse(
                TestBeatmap.Code(
                    ["0,100,1000,2,0,L|280:100,1000,140"],
                    timingPoints: ["0,500,4,2,0,100,1,0", "2100,400,4,3,0,100,1,0"]
                )
            )
            .HitObjects[0];
        var body = slider.usedHitSamples.Where(s => s.HitSource == HitSourceType.Body).ToList();

        Assert.True(body.Count > 500);
        Assert.Contains(body, s => s.Sampleset == SamplesetType.Drum);
    }

    [Theory]
    [InlineData("audio.mp3")]
    [InlineData("")]
    public void AudioFile_ThatIsMissing_IsNone(string name)
    {
        Assert.Null(
            TestBeatmap
                .Parse(TestBeatmap.Code(general: "AudioFilename: " + name))
                .GetAudioFilePath()
        );
    }

    [Fact]
    public void ScaledBpm_WithoutRedLines_IsZero()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code([TestBeatmap.Circle(1000)], timingPoints: [GreenLine], mode: 2)
        );

        Assert.Equal(
            0,
            Parser.Objects.HitObjects.Catch.CatchExtensions.GetScaledBpm(
                beatmap,
                (Parser.Objects.HitObjects.Catch.ICatchHitObject)beatmap.HitObjects[0]
            )
        );
    }

    [Theory]
    [InlineData(500, 60, 60)]
    [InlineData(400, 120, 40)]
    [InlineData(250, 160, 80)]
    [InlineData(180, 220, 80)]
    [InlineData(150, 300, 80)]
    [InlineData(120, 400, 80)]
    public void StarRatingTiers_FollowTheRating(int interval, int distance, int count)
    {
        var beatmap = TestBeatmap.WithObjects([
            .. Enumerable
                .Range(0, count)
                .Select(i => TestBeatmap.Circle(1000 + i * interval, x: i % 2 * distance)),
        ]);
        var stars = beatmap.StarRating;
        var expected =
            stars < 2.0f ? Beatmap.Difficulty.Easy
            : stars < 2.7f ? Beatmap.Difficulty.Normal
            : stars < 4.0f ? Beatmap.Difficulty.Hard
            : stars < 5.3f ? Beatmap.Difficulty.Insane
            : stars < 6.5f ? Beatmap.Difficulty.Expert
            : Beatmap.Difficulty.Ultra;

        Assert.Equal(expected, beatmap.GetDifficulty());
    }
}
