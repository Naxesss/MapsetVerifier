using System.Globalization;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects;

public class SliderVelocityTests
{
    // A beat lasts 333.33 ms and covers 240 px at 1x, so a 480 px slider lasts 666.67 ms at 1x and 333.33 ms at 2x.
    private static Slider CreateSlider(double greenLineOffset)
    {
        var code = $"""
osu file format v14

[General]
Mode: 0

[Metadata]
Version:Test

[Difficulty]
CircleSize:4
HPDrainRate:5
OverallDifficulty:5
ApproachRate:5
SliderMultiplier:2.4
SliderTickRate:1

[TimingPoints]
0,333.333333333333,4,2,0,100,1,0
{greenLineOffset.ToString(CultureInfo.InvariantCulture)},-50,4,2,0,100,0,0

[HitObjects]
0,192,1000,2,0,L|512:192,1,480
""";

        // Timing lines are cached per path, so each slider needs its own.
        return (Slider)new Beatmap(code, "song", $"{Guid.NewGuid()}.osu").HitObjects.First();
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(1000.33)]
    [InlineData(1000.98)]
    public void GreenLineWithinTheSameMillisecondAppliesToTheSlider(double greenLineOffset)
    {
        var slider = CreateSlider(greenLineOffset);

        Assert.InRange(slider.EndTime, 1333, 1334);
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(1001.2)]
    public void GreenLineInALaterMillisecondDoesNotApplyToTheSlider(double greenLineOffset)
    {
        var slider = CreateSlider(greenLineOffset);

        Assert.InRange(slider.EndTime, 1666, 1668);
    }
}
