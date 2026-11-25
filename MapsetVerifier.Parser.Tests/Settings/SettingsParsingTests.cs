using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Settings;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Settings;

public class SettingsParsingTests
{
    [Fact]
    public void General_AllFields()
    {
        var settings = new GeneralSettings([
            "AudioFilename: my song.mp3",
            "AudioLeadIn: 1500",
            "PreviewTime: 160876",
            "Countdown: 3",
            "CountdownOffset: 2",
            "SampleSet: Soft",
            "StackLeniency: 0.2",
            "Mode: 3",
            "LetterboxInBreaks: 1",
            "StoryFireInFront: 1",
            "UseSkinSprites: 1",
            "EpilepsyWarning: 1",
            "SpecialStyle: 1",
            "WidescreenStoryboard: 1",
            "SkinPreference:My Skin",
        ]);

        Assert.Equal("my song.mp3", settings.audioFileName);
        Assert.Equal(1500, settings.audioLeadIn);
        Assert.Equal(160876, settings.previewTime);
        Assert.Equal(GeneralSettings.Countdown.Double, settings.countdown);
        Assert.Equal(2, settings.countdownBeatOffset);
        Assert.Equal(2, settings.stackLeniency, 0.001f);
        Assert.Equal(Beatmap.Mode.Mania, settings.mode);
        Assert.True(settings.letterbox);
        Assert.True(settings.storyInFrontOfFire);
        Assert.True(settings.useSkinSprites);
        Assert.True(settings.epilepsyWarning);
        Assert.True(settings.specialN1Style);
        Assert.True(settings.widescreenSupport);
        Assert.Equal("My Skin", settings.skinPreference);
    }

    [Fact]
    public void General_Defaults()
    {
        var settings = new GeneralSettings([]);

        Assert.Equal(0, settings.audioLeadIn);
        Assert.Equal(-1, settings.previewTime);
        Assert.Equal(7, settings.stackLeniency, 0.001f);
        Assert.Equal(Beatmap.Mode.Standard, settings.mode);
        Assert.False(settings.letterbox);
        Assert.False(settings.widescreenSupport);
        Assert.False(settings.epilepsyWarning);
    }

    [Fact]
    public void General_CountdownOffsetAlone_IsNotTheCountdown()
    {
        var settings = new GeneralSettings(["CountdownOffset: 2"]);

        Assert.Equal(2, settings.countdownBeatOffset);
        Assert.NotEqual(GeneralSettings.Countdown.Half, settings.countdown);
    }

    [Fact]
    public void General_MissingCountdown_IsNormal()
    {
        // The default according to the file format.
        Assert.Equal(GeneralSettings.Countdown.Normal, new GeneralSettings([]).countdown);
    }

    [Fact]
    public void Difficulty_AllFields()
    {
        var settings = new DifficultySettings([
            "HPDrainRate:6",
            "CircleSize:4.2",
            "OverallDifficulty:9",
            "ApproachRate:9.7",
            "SliderMultiplier:2.5",
            "SliderTickRate:2",
        ]);

        Assert.Equal(6, settings.hpDrain);
        Assert.Equal(4.2f, settings.circleSize);
        Assert.Equal(9, settings.overallDifficulty);
        Assert.Equal(9.7f, settings.approachRate);
        Assert.Equal(2.5f, settings.sliderMultiplier);
        Assert.Equal(2, settings.sliderTickRate);
    }

    [Fact]
    public void Difficulty_IsLimitedToWhatTheGamePlays()
    {
        var high = new DifficultySettings([
            "HPDrainRate:11",
            "OverallDifficulty:12.5",
            "ApproachRate:100",
            "SliderMultiplier:9",
            "SliderTickRate:20",
        ]);

        Assert.Equal(10, high.hpDrain);
        Assert.Equal(10, high.overallDifficulty);
        Assert.Equal(10, high.approachRate);
        Assert.Equal(3.6f, high.sliderMultiplier);
        Assert.Equal(8, high.sliderTickRate);

        var low = new DifficultySettings([
            "HPDrainRate:-1",
            "SliderMultiplier:0.1",
            "SliderTickRate:0",
        ]);

        Assert.Equal(0, low.hpDrain);
        Assert.Equal(0.4f, low.sliderMultiplier);
        Assert.Equal(0.5f, low.sliderTickRate);
    }

    [Fact]
    public void Difficulty_MissingApproachRate_IsTheOverallDifficulty()
    {
        // Approach rate was split off from overall difficulty in format v8.
        Assert.Equal(7, new DifficultySettings(["OverallDifficulty:7"]).approachRate);
    }

    [Theory]
    [InlineData(0, 54.4)]
    [InlineData(4, 36.48)]
    [InlineData(5, 32)]
    [InlineData(10, 9.6)]
    public void CircleRadius(float circleSize, double expected)
    {
        // 54.4 - 4.48 * CS
        Assert.Equal(expected, DifficultySettings.GetCircleRadius(circleSize), 3);
        Assert.Equal(
            expected,
            new DifficultySettings([$"CircleSize:{circleSize}"]).GetCircleRadius(),
            3
        );
    }

    [Theory]
    [InlineData(0, 1800)]
    [InlineData(5, 1200)]
    [InlineData(8, 750)]
    [InlineData(10, 450)]
    public void DifficultyRange_InterpolatesOnBothSidesOfTheMiddle(double ar, double expected)
    {
        Assert.Equal(expected, DifficultySettings.DifficultyRange(ar, (450, 1200, 1800)), 6);
    }

    [Fact]
    public void Colours_AreInFileOrder()
    {
        var settings = new ColourSettings([
            "Combo1 : 129,176,182",
            "Combo2:255, 40 ,40",
            "SliderBorder : 1,2,3",
            "SliderTrackOverride : 4,5,6",
        ]);

        Assert.Equal([new Vector3(129, 176, 182), new Vector3(255, 40, 40)], settings.combos);
        Assert.Equal(new Vector3(1, 2, 3), settings.sliderBorder);
        Assert.Equal(new Vector3(4, 5, 6), settings.sliderTrackOverride);
    }

    [Fact]
    public void Colours_AreOptional()
    {
        var settings = new ColourSettings([]);

        Assert.Empty(settings.combos);
        Assert.Null(settings.sliderBorder);
        Assert.Null(settings.sliderTrackOverride);
    }

    [Fact]
    public void Metadata_FieldsSharingAPrefix_AreKeptApart()
    {
        var settings = new MetadataSettings([
            "Title:Yuumeikyou: Remix",
            "TitleUnicode:幽明境",
            "Artist:Diao Ye Zong",
            "ArtistUnicode:凋叶棕",
        ]);

        Assert.Equal("Yuumeikyou: Remix", settings.title);
        Assert.Equal("幽明境", settings.titleUnicode);
        Assert.Equal("Diao Ye Zong", settings.artist);
        Assert.Equal("凋叶棕", settings.artistUnicode);
    }

    [Fact]
    public void Metadata_FieldsSharingAPrefix_AreKeptApart_InAnyOrder()
    {
        var settings = new MetadataSettings([
            "TitleUnicode:幽明境",
            "Title:Yuumeikyou",
            "ArtistUnicode:凋叶棕",
            "Artist:Diao Ye Zong",
        ]);

        Assert.Equal("Yuumeikyou", settings.title);
        Assert.Equal("Diao Ye Zong", settings.artist);
    }

    [Fact]
    public void Metadata_Defaults()
    {
        var settings = new MetadataSettings(["Title:Song", "Artist:Band"]);

        // Files before v10 have no unicode fields.
        Assert.Equal("Song", settings.titleUnicode);
        Assert.Equal("Band", settings.artistUnicode);
        Assert.Equal("", settings.source);
        Assert.Equal("", settings.tags);
        Assert.Null(settings.beatmapId);
        Assert.Null(settings.beatmapSetId);
    }
}
