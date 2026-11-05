using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects;

public class SliderTickTests
{
    private const int Precision = 3;

    // A beat lasts 333.33 ms and covers 240 px at 1x, so a slider moves 0.72 px/ms.
    private static Slider CreateSlider(
        string pixelLengthAndSlides,
        int tickRate = 1,
        int fileFormat = 14,
        double svLineBeatLength = -100,
        int mode = 0
    )
    {
        var (pixelLength, slides) = (
            pixelLengthAndSlides.Split(':')[0],
            pixelLengthAndSlides.Split(':')[1]
        );
        var code = $"""
osu file format v{fileFormat}

[General]
Mode: {mode}

[Metadata]
Version:Test

[Difficulty]
CircleSize:4
HPDrainRate:5
OverallDifficulty:5
ApproachRate:5
SliderMultiplier:2.4
SliderTickRate:{tickRate}

[TimingPoints]
0,333.333333333333,4,2,0,100,1,0
500,{svLineBeatLength.ToString(System.Globalization.CultureInfo.InvariantCulture)},4,2,0,100,0,0

[HitObjects]
0,192,1000,2,0,L|512:192,{slides},{pixelLength}
""";

        // Timing lines are cached per path, so each slider needs its own.
        return (Slider)new Beatmap(code, "song", $"{Guid.NewGuid()}.osu").HitObjects.First();
    }

    private static void AssertTicks(Slider slider, params double[] expected)
    {
        Assert.Equal(expected.Length, slider.SliderTickTimes.Count);
        for (var i = 0; i < expected.Length; ++i)
            Assert.Equal(expected[i], slider.SliderTickTimes[i], Precision);
    }

    [Fact]
    public void SingleSpan_TicksFollowTheTickRate_AndKeepDecimals()
    {
        // There is no tick on the tail itself, which is 480 px along the path.
        AssertTicks(CreateSlider("480:1", tickRate: 2), 1166.667, 1333.333, 1500);
    }

    [Fact]
    public void ReverseSpan_MirrorsTicksInTime()
    {
        // The path is 300 px long with a tick at 240 px. Going back, that point is reached 83.33 ms
        // into the span, instead of a full tick interval after the previous tick.
        AssertTicks(CreateSlider("300:2"), 1333.333, 1500);
    }

    [Fact]
    public void ReverseSpanOnTheTickGrid_MatchesCountingFromTheStart()
    {
        AssertTicks(CreateSlider("720:2"), 1333.333, 1666.667, 2333.333, 2666.667);
    }

    [Theory]
    // 240 px is within 10 ms (7.2 px) of the end of a 245 px path, but not of a 250 px path.
    [InlineData("245:1", 0)]
    [InlineData("250:1", 1)]
    // The rule applies per span, so a reverse slider loses the tick in both directions.
    [InlineData("245:2", 0)]
    [InlineData("250:2", 2)]
    public void TickNearTheEndOfThePath_IsSkipped(string lengthAndSlides, int expectedTicks)
    {
        Assert.Equal(expectedTicks, CreateSlider(lengthAndSlides).SliderTickTimes.Count);
    }

    [Theory]
    // From file format 8, a tick interval lasts the same time at any velocity. At 2x this path takes
    // 333.33 ms and ends right as the first tick would land, so it has none.
    [InlineData(14, new double[0])]
    // Before that, ticks keep the same distance, so at 2x they arrive twice as fast.
    [InlineData(7, new[] { 1166.667 })]
    public void SliderVelocity_AffectsTickSpacingOnlyInOldFileFormats(
        int fileFormat,
        double[] expected
    )
    {
        AssertTicks(CreateSlider("480:1", fileFormat: fileFormat, svLineBeatLength: -50), expected);
    }

    [Fact]
    public void InvalidLength_HasNoTicks()
    {
        Assert.Empty(CreateSlider("-10:1").SliderTickTimes);
    }

    [Fact]
    public void JuiceStreamDroplet_OnReverseSpan_SitsAtTheSamePlaceOnThePath()
    {
        var stream = Assert.IsType<JuiceStream>(CreateSlider("300:2", mode: 2));
        var droplets = stream
            .Parts.Where(part => part.Kind == JuiceStream.JuiceStreamPart.PartKind.Droplet)
            .ToList();

        Assert.Equal(2, droplets.Count);
        Assert.Equal(1500, droplets[1].Time, Precision);
        Assert.Equal(droplets[0].Position.X, droplets[1].Position.X, 1);
    }
}
