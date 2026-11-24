using MapsetVerifier.Parser.Objects;
using Xunit;
using static MapsetVerifier.Parser.Objects.HitObject;
using static MapsetVerifier.Parser.Objects.HitSample;

namespace MapsetVerifier.Parser.Tests.Objects;

public class HitSampleTests
{
    [Theory]
    [InlineData("soft-hitnormal.wav", SamplesetType.Soft, HitSounds.Normal, 1, false)]
    [InlineData("drum-hitfinish3.ogg", SamplesetType.Drum, HitSounds.Finish, 3, false)]
    [InlineData("normal-hitwhistle12.wav", SamplesetType.Normal, HitSounds.Whistle, 12, false)]
    [InlineData("taiko-drum-hitnormal2.wav", SamplesetType.Drum, HitSounds.Normal, 2, true)]
    [InlineData("Soft-HitNormal2.WAV", SamplesetType.Soft, HitSounds.Normal, 2, false)]
    public void FileName_OfAnEdgeSample_IsParsed(
        string fileName,
        SamplesetType sampleset,
        HitSounds hitSound,
        int customIndex,
        bool taiko
    )
    {
        var sample = new HitSample(fileName);

        Assert.Equal(sampleset, sample.Sampleset);
        Assert.Equal(hitSound, sample.HitSound);
        Assert.Equal(customIndex, sample.CustomIndex);
        Assert.Equal(taiko, sample.Taiko);
        Assert.Equal(HitSourceType.Edge, sample.HitSource);
    }

    [Fact]
    public void FileName_OfAClap_IsParsed()
    {
        var sample = new HitSample("soft-hitclap2.wav");

        Assert.Equal(HitSounds.Clap, sample.HitSound);
        Assert.Equal(2, sample.CustomIndex);
    }

    [Theory]
    [InlineData("soft-sliderslide2.wav", HitSourceType.Body)]
    [InlineData("soft-sliderwhistle2.wav", HitSourceType.Body)]
    [InlineData("soft-slidertick2.wav", HitSourceType.Tick)]
    public void FileName_OfASliderSample_IsParsed(string fileName, HitSourceType source)
    {
        var sample = new HitSample(fileName);

        Assert.Equal(source, sample.HitSource);
        Assert.Equal(2, sample.CustomIndex);
    }

    [Theory]
    [InlineData("soft-hitnormal2.wav")]
    [InlineData("soft-hitclap2.wav")]
    [InlineData("soft-sliderslide2.wav")]
    [InlineData("soft-sliderwhistle2.wav")]
    [InlineData("soft-slidertick2.wav")]
    public void ParsedFileName_RoundTrips(string fileName)
    {
        Assert.True(new HitSample(fileName).SameFileName(fileName));
    }

    [Theory]
    [InlineData("audio.mp3")]
    [InlineData("my-soft-hitnormal.wav")]
    [InlineData("")]
    public void FileName_OfSomethingElse_HasNoSample(string fileName)
    {
        var sample = new HitSample(fileName);

        Assert.Null(sample.Sampleset);
        Assert.Null(sample.HitSound);
        Assert.Equal(HitSourceType.Unknown, sample.HitSource);
        Assert.Null(sample.GetFileName());
    }

    [Theory]
    [InlineData(
        0,
        SamplesetType.Soft,
        HitSounds.Normal,
        HitSourceType.Edge,
        false,
        "soft-hitnormal"
    )]
    // The first custom index has no number in its file name.
    [InlineData(1, SamplesetType.Soft, HitSounds.Clap, HitSourceType.Edge, false, "soft-hitclap")]
    [InlineData(
        2,
        SamplesetType.Drum,
        HitSounds.Finish,
        HitSourceType.Edge,
        true,
        "taiko-drum-hitfinish2"
    )]
    [InlineData(
        5,
        SamplesetType.Normal,
        HitSounds.None,
        HitSourceType.Body,
        false,
        "normal-sliderslide5"
    )]
    [InlineData(
        5,
        SamplesetType.Normal,
        HitSounds.Whistle,
        HitSourceType.Body,
        false,
        "normal-sliderwhistle5"
    )]
    [InlineData(1, SamplesetType.Soft, null, HitSourceType.Tick, false, "soft-slidertick")]
    [InlineData(1, SamplesetType.Soft, HitSounds.None, HitSourceType.Edge, false, null)]
    [InlineData(1, null, HitSounds.Normal, HitSourceType.Edge, false, null)]
    public void FileName_IsBuiltFromTheSample(
        int customIndex,
        SamplesetType? sampleset,
        HitSounds? hitSound,
        HitSourceType source,
        bool taiko,
        string? expected
    )
    {
        var sample = new HitSample(customIndex, sampleset, hitSound, source, 0, taiko);

        Assert.Equal(expected, sample.GetFileName());
        // The name is cached, so asking again must give the same.
        Assert.Equal(expected, sample.GetFileName());
    }

    [Theory]
    [InlineData("soft-hitnormal2.wav", true)]
    [InlineData("SOFT-HITNORMAL2.OGG", true)]
    [InlineData("soft-hitnormal.wav", false)]
    [InlineData("soft-hitnormal22.wav", false)]
    public void SameFileName_IgnoresCaseAndExtension(string fileName, bool expected)
    {
        var sample = new HitSample(2, SamplesetType.Soft, HitSounds.Normal, HitSourceType.Edge, 0);

        Assert.Equal(expected, sample.SameFileName(fileName));
    }
}
