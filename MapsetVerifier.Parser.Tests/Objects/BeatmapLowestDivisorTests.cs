using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

public class BeatmapLowestDivisorTests
{
    // 168 BPM with a red line at 49983, as in "Trial & Error - Ashita e tobutameni (TV Size)".
    // There the 1/9 grid point at a beat truncates to 0 ms off while the 1/1 point truncates to 1 ms off,
    // which used to report a plain 1/1 note as 1/9.
    private static Beatmap CreateBeatmap() =>
        new(
            """
            osu file format v14

            [General]
            Mode: 2

            [Metadata]
            Version:Test

            [Difficulty]
            SliderMultiplier:1.79
            SliderTickRate:1

            [TimingPoints]
            49983,357.142857142857,4,2,0,100,1,0

            [HitObjects]
            256,192,49983,1,0,0:0:0:0:
            """,
            "song",
            $"{Guid.NewGuid()}.osu"
        );

    [Theory]
    [InlineData(69982, 1)]
    [InlineData(71232, 2)]
    [InlineData(69179, 4)]
    public void SharedGridPoint_ReturnsLowestDivisor(double time, int expected)
    {
        Assert.Equal(expected, CreateBeatmap().GetLowestDivisor(time));
    }

    [Fact]
    public void UnsnappedTime_ReturnsZero()
    {
        Assert.Equal(0, CreateBeatmap().GetLowestDivisor(49983 + 10));
    }
}
