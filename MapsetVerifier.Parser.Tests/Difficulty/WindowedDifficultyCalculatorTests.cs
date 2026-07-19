using System.Text;
using MapsetVerifier.Parser.Difficulty;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Difficulty;

public class WindowedDifficultyCalculatorTests
{
    private static Beatmap CreateBeatmapWithSpike()
    {
        var header = """
osu file format v14

[General]
AudioFilename: audio.mp3
Mode: 0

[Metadata]
Version:Spike test

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:8
ApproachRate:9
SliderMultiplier:1.4
SliderTickRate:1

[TimingPoints]
0,333.333333333333,4,2,1,50,1,0

[HitObjects]
""";

        var sb = new StringBuilder(header);

        // Easy section: 0 - 10000ms, a circle every 666ms alternating x position.
        for (var time = 0; time < 10000; time += 666)
            sb.Append($"\n{(time % 1332 == 0 ? 100 : 300)},192,{time},1,0,0:0:0:0:");

        // Hard section: 10000 - 15000ms, a fast alternating-jump stream every 90ms.
        for (var time = 10000; time < 15000; time += 90)
            sb.Append($"\n{(time % 180 == 0 ? 30 : 470)},192,{time},1,0,0:0:0:0:");

        // Easy section again: 15000 - 25000ms.
        for (var time = 15000; time < 25000; time += 666)
            sb.Append($"\n{(time % 1332 == 0 ? 100 : 300)},192,{time},1,0,0:0:0:0:");

        return new Beatmap(sb.ToString(), "song", "map.osu");
    }

    [Fact]
    public void CalculateWindowedStarRatings_SpikesDuringHardSection()
    {
        var beatmap = CreateBeatmapWithSpike();

        var samples = new WindowedDifficultyCalculator().CalculateWindowedStarRatings(
            beatmap,
            windowMs: 4000,
            strideMs: 2000,
            maxPoints: 20
        );

        Assert.NotEmpty(samples);

        var easyPeak = samples
            .Where(sample => sample.TimeMs is <= 8000 or >= 21000)
            .Max(sample => sample.StarRating);
        var hardPeak = samples
            .Where(sample => sample.TimeMs is >= 12000 and <= 15000)
            .Max(sample => sample.StarRating);

        Assert.True(
            hardPeak > easyPeak,
            $"Expected the hard section's local star rating ({hardPeak}) to spike above the easy sections' ({easyPeak})."
        );
    }
}
