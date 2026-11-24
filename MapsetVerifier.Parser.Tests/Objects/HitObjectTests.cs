using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using Xunit;
using static MapsetVerifier.Parser.Objects.HitObject;
using static MapsetVerifier.Parser.Objects.HitSample;

namespace MapsetVerifier.Parser.Tests.Objects;

public class HitObjectTests
{
    // 1.4 base velocity at 500 ms per beat, so 140 px take one beat.
    private const string ReverseSlider =
        "0,100,1000,2,0,L|280:100,2,140,2|4|8,1:2|3:1|0:0,2:3:0:0:";

    private static HitObject Parse(string line, int mode = 0) =>
        Assert.Single(TestBeatmap.Parse(TestBeatmap.Code([line], mode: mode)).HitObjects);

    [Fact]
    public void Circle_AllFields()
    {
        var circle = Parse("131,304,1166,5,10,1:2:3:40:Custom Sound.wav");

        Assert.IsType<Circle>(circle);
        Assert.Equal(new Vector2(131, 304), circle.Position);
        Assert.Equal(1166, circle.time);
        Assert.True(circle.HasType(Types.NewCombo));
        Assert.Equal(SamplesetType.Normal, circle.sampleset);
        Assert.Equal(SamplesetType.Soft, circle.addition);
        Assert.Equal(3, circle.customIndex);
        Assert.Equal(40, circle.volume);
        Assert.Equal("Custom Sound.wav", circle.filename);
        Assert.Equal([HitSounds.Whistle, HitSounds.Clap], circle.GetHitSounds());
        Assert.Equal(1166, circle.GetEndTime());
        Assert.Equal(0, circle.GetLength());
    }

    [Fact]
    public void Circle_WithoutExtras_UsesTheTimingLine()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(["256,192,1000,1,0"], timingPoints: ["0,500,4,3,7,100,1,0"])
        );
        var circle = beatmap.HitObjects[0];

        Assert.Null(circle.customIndex);
        Assert.Null(circle.volume);
        Assert.Null(circle.filename);
        Assert.Equal(SamplesetType.Drum, circle.GetSampleset());
        Assert.Equal(7, circle.GetCustomIndex());
        Assert.Contains("drum-hitnormal7", circle.GetUsedHitSoundFileNames());
    }

    [Fact]
    public void ExtrasWithoutVolume_AsInOldFormats_AreParsed()
    {
        var circle = Parse("256,192,1000,1,0,2:1:0");

        Assert.Equal(SamplesetType.Soft, circle.sampleset);
        Assert.Equal(SamplesetType.Normal, circle.addition);
        Assert.Null(circle.volume);
    }

    [Fact]
    public void HoldNote_EndTimeIsInFrontOfTheExtras()
    {
        var hold = Assert.IsType<HoldNote>(
            Parse("448,192,1000,128,2,3500:1:2:3:40:hold.wav", mode: 3)
        );

        Assert.Equal(3500, hold.GetEndTime());
        Assert.Equal(2500, hold.GetLength());
        Assert.Equal(SamplesetType.Normal, hold.sampleset);
        Assert.Equal(SamplesetType.Soft, hold.addition);
        Assert.Equal(3, hold.customIndex);
        Assert.Equal(40, hold.volume);
        Assert.Equal("hold.wav", hold.filename);
        Assert.Equal([1000, 3500], hold.GetEdgeTimes());
    }

    [Fact]
    public void Spinner_OnlySoundsWhenItEnds()
    {
        var spinner = Assert.IsType<Spinner>(Parse("256,192,1000,12,4,3000,1:0:0:0:"));

        Assert.Equal(3000, spinner.GetEndTime());
        Assert.Equal([1000, 3000], spinner.GetEdgeTimes());
        Assert.Null(spinner.GetStartHitSound());
        Assert.Null(spinner.GetStartSampleset());
        Assert.Equal(HitSounds.Finish, spinner.GetEndHitSound());
        Assert.Equal(SamplesetType.Normal, spinner.GetEndSampleset());
        Assert.All(
            spinner.usedHitSamples.Where(sample => !sample.Taiko),
            sample => Assert.Equal(3000, sample.Time)
        );
    }

    [Fact]
    public void Slider_EdgesKeepTheirOwnHitSoundsAndSamplesets()
    {
        var slider = Assert.IsType<Slider>(Parse(ReverseSlider));

        Assert.Equal(2, slider.EdgeAmount);
        Assert.Equal(HitSounds.Whistle, slider.StartHitSound);
        Assert.Equal([HitSounds.Finish], slider.ReverseHitSounds);
        Assert.Equal(HitSounds.Clap, slider.EndHitSound);

        Assert.Equal(SamplesetType.Normal, slider.GetStartSampleset());
        Assert.Equal(SamplesetType.Soft, slider.GetStartSampleset(true));
        Assert.Equal(SamplesetType.Drum, slider.GetReverseSampleset(0));
        Assert.Equal(SamplesetType.Normal, slider.GetReverseSampleset(0, true));
        // Auto on the tail, so the timing line's soft.
        Assert.Equal(SamplesetType.Soft, slider.GetEndSampleset());
        Assert.Equal(SamplesetType.Soft, slider.GetEndSampleset(true));
    }

    [Fact]
    public void Slider_EdgeTimes_AreHeadReversesAndTail()
    {
        var slider = Parse(ReverseSlider);
        var edges = slider.GetEdgeTimes().ToList();

        Assert.Equal(3, edges.Count);
        Assert.Equal(1000, edges[0], 3);
        Assert.Equal(1500, edges[1], 3);
        Assert.Equal(2000, edges[2], 3);
        Assert.Equal(2000, slider.GetEndTime(), 3);
    }

    [Fact]
    public void Slider_WithoutEdgeFields_UsesTheObjectsHitSoundOnAllEdges()
    {
        var slider = Assert.IsType<Slider>(Parse("0,100,1000,2,8,L|280:100,2,140"));

        Assert.Equal(HitSounds.Clap, slider.StartHitSound);
        Assert.Equal(HitSounds.Clap, slider.ReverseHitSounds[0]);
        Assert.Equal(HitSounds.Clap, slider.EndHitSound);
        Assert.Equal(HitSounds.Clap, slider.GetSliderSlide());
    }

    [Fact]
    public void Slider_WithoutHitSample_IsParsed()
    {
        // The hit sample at the end is optional and defaults to 0:0:0:0:.
        var slider = Assert.IsType<Slider>(Parse("0,100,1000,2,0,L|280:100,1,140,2|0,1:2|0:0"));

        Assert.Equal(HitSounds.Whistle, slider.StartHitSound);
        Assert.Equal(SamplesetType.Normal, slider.GetStartSampleset());
        Assert.Equal(SamplesetType.Auto, slider.sampleset);
    }

    [Fact]
    public void Slider_FileNameInExtras_IsIgnored()
    {
        // Custom files only apply to circles and hold notes.
        Assert.Null(Parse("0,100,1000,2,0,L|280:100,1,140,0|0,0:0|0:0,0:0:0:0:a.wav").filename);
    }

    [Fact]
    public void Slider_UsesSlideTickAndEdgeFiles()
    {
        // Two beats long, with a tick after the first.
        var names = Parse("0,100,1000,2,2,L|380:100,1,280,0|8,0:0|0:0,0:0:0:0:")
            .GetUsedHitSoundFileNames()
            .ToList();

        Assert.Contains("soft-hitnormal", names);
        Assert.Contains("soft-hitclap", names);
        Assert.Contains("soft-slidertick", names);
        Assert.Contains("soft-sliderslide", names);
        Assert.Contains("soft-sliderwhistle", names);
        Assert.DoesNotContain("soft-hitfinish", names);
    }

    [Fact]
    public void Slider_AlsoUsesTheIndexesOfLinesStartingInsideOfIt()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                ["0,100,1000,2,0,L|380:100,1,280,0|0,0:0|0:0,0:0:0:0:"],
                timingPoints: ["0,500,4,2,1,100,1,0", "1300,-100,4,3,5,100,0,0"]
            )
        );
        var names = beatmap.HitObjects[0].GetUsedHitSoundFileNames().ToList();

        Assert.Contains("soft-sliderslide", names);
        Assert.Contains("drum-sliderslide5", names);
        Assert.Contains("drum-slidertick5", names);
    }

    [Fact]
    public void CustomFile_ReplacesAllOtherSounds()
    {
        Assert.Equal(
            ["my sound"],
            Parse("256,192,1000,1,15,0:0:0:0:my sound.wav").GetUsedHitSoundFileNames()
        );
    }

    [Theory]
    [InlineData(0, "taiko-soft-hitnormal")]
    [InlineData(4, "taiko-soft-hitfinish")] // Big don.
    [InlineData(8, "taiko-soft-hitclap")] // Kat, by clap
    [InlineData(2, "taiko-soft-hitclap")] // or by whistle.
    [InlineData(12, "taiko-soft-hitwhistle")] // Big kat.
    [InlineData(6, "taiko-soft-hitwhistle")]
    public void Taiko_NoteTypeDecidesTheFile(int hitSound, string expected)
    {
        var names = Parse($"256,192,1000,1,{hitSound},0:0:0:0:", mode: 1)
            .GetUsedHitSoundFileNames()
            .ToList();

        Assert.Contains(expected, names);
        // Played on every key press.
        Assert.Contains("taiko-soft-hitnormal", names);
        Assert.Contains("taiko-soft-hitclap", names);
        Assert.All(names, name => Assert.StartsWith("taiko-", name));
    }

    [Fact]
    public void Standard_CanBeConverted_SoUsesTaikoFilesToo()
    {
        var names = Parse(TestBeatmap.Circle(1000)).GetUsedHitSoundFileNames().ToList();

        Assert.Contains("taiko-soft-hitnormal", names);
        Assert.Contains("soft-hitnormal", names);
    }

    [Theory]
    [InlineData(1000, "Slider head")]
    [InlineData(1002, "Slider head")]
    [InlineData(1250, "Slider body")]
    [InlineData(1500, "Slider reverse")]
    [InlineData(2000, "Slider tail")]
    public void PartName_OfASlider(double time, string expected)
    {
        Assert.Equal(expected, Parse(ReverseSlider).GetPartName(time));
    }

    [Fact]
    public void PartName_OfOtherObjects()
    {
        Assert.Equal("Circle", Parse(TestBeatmap.Circle(1000)).GetPartName(1000));

        var spinner = Parse("256,192,1000,12,0,3000,0:0:0:0:");
        Assert.Equal("Spinner head", spinner.GetPartName(1000));
        Assert.Equal("Spinner tail", spinner.GetPartName(3000));

        var hold = Parse("64,192,1000,128,0,3000:0:0:0:0:", mode: 3);
        Assert.Equal("Hold note tail", hold.GetPartName(3000));
    }

    [Fact]
    public void PrevDistanceAndDelta_AreMeasuredFromTheSlidersEnd()
    {
        var beatmap = TestBeatmap.WithObjects(
            "0,100,1000,2,0,L|280:100,1,140,0|0,0:0|0:0,0:0:0:0:",
            TestBeatmap.Circle(2000, x: 140, y: 200)
        );
        var (slider, circle) = (beatmap.HitObjects[0], beatmap.HitObjects[1]);

        Assert.Equal(500, circle.GetPrevDeltaTime(slider), 3);
        Assert.Equal(100, circle.GetPrevDistance(slider), 0);
    }

    [Theory]
    [InlineData(1, "Circle")]
    [InlineData(2, "Slider")]
    [InlineData(8, "Spinner")]
    [InlineData(128, "Hold note")]
    [InlineData(4, "Unknown object")]
    public void ObjectType_OfABareHitObject(int type, string expected)
    {
        var hitObject = new HitObject(
            ["0", "0", "1000", type.ToString(), "0"],
            TestBeatmap.WithObjects()
        );

        Assert.Equal(expected, hitObject.GetObjectType());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(8, true)]
    public void HitSound_OfAnyKind(int hitSound, bool expected)
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(hitObjects: [$"256,192,1000,1,{hitSound},0:0:0:0:"])
        );

        Assert.Equal(expected, beatmap.HitObjects[0].HasHitSound());
        Assert.Equal(hitSound == 8, beatmap.HitObjects[0].HasHitSound(HitObject.HitSounds.Clap));
    }
}
