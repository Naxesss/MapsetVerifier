using MapsetVerifier.Checks.Tests.Mania;
using Xunit;

namespace MapsetVerifier.Checks.Tests;

public class TestFormatHelpersTests
{
    [Fact]
    public void Uninherited_DefaultMatchesStandardGolden()
    {
        Assert.Equal("0,500,4,2,0,100,1,0", TestTimingPoints.Uninherited(0, 500));
        Assert.Equal("0,500,4,2,0,100,1,0", OsuBuilder.DefaultTimingPoint);
    }

    [Fact]
    public void Uninherited_ManiaSampleSetMatchesGolden()
    {
        Assert.Equal("0,500,4,1,0,100,1,0", TestTimingPoints.Uninherited(0, 500, sampleSet: 1));
        Assert.Equal("0,500,4,1,0,100,1,0", ManiaOsu.NormalTimingPoint);
    }

    [Fact]
    public void Inherited_NegativeBeatLengthAndKiai()
    {
        Assert.Equal("1000,-50,4,2,0,100,0,1", TestTimingPoints.Inherited(1000, -50, effects: 1));
    }

    [Fact]
    public void Uninherited_CustomVolume()
    {
        Assert.Equal("0,500,4,2,0,50,1,0", TestTimingPoints.Uninherited(0, 500, volume: 50));
    }

    [Fact]
    public void Circle_DefaultMatchesGolden()
    {
        Assert.Equal("256,192,1000,1,0,0:0:0:0:", TestHitObjects.Circle(1000));
        Assert.Equal("256,192,1000,1,0,0:0:0:0:", OsuBuilder.DefaultHitObject);
    }

    [Fact]
    public void Slider_DefaultMatchesCommonStub()
    {
        Assert.Equal(
            "256,192,1000,2,0,L|256:300,1,100,0|0,0:0|0:0,0:0:0:0:",
            TestHitObjects.Slider(1000)
        );
    }

    [Fact]
    public void Spinner_EmitsEndTimeAndHitSample()
    {
        Assert.Equal("256,192,1000,8,0,2000,0:0:0:0:", TestHitObjects.Spinner(1000, 2000));
    }

    [Fact]
    public void Helpers_ParseThroughOsuBuilder()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Format Helpers")
                .TimingPoints(
                    TestTimingPoints.Uninherited(0, 500),
                    TestTimingPoints.Inherited(1000, -50)
                )
                .HitObjects(
                    TestHitObjects.Circle(500),
                    TestHitObjects.Slider(1000),
                    TestHitObjects.Spinner(2000, 3000)
                )
        );

        var beatmap = Assert.Single(context.BeatmapSet.Beatmaps);
        Assert.Equal(2, beatmap.TimingLines.Count);
        Assert.Equal(3, beatmap.HitObjects.Count);
    }
}
