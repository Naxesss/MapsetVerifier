using System.Globalization;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Statics;

public class TimestampTests
{
    [Theory]
    // Column centres in 4K and 10K.
    [InlineData(4, 64, 0)]
    [InlineData(4, 448, 3)]
    [InlineData(10, 25, 0)]
    [InlineData(10, 486, 9)]
    // Off-centre positions, as produced by converts and some editors.
    [InlineData(10, 42, 0)]
    [InlineData(10, 469, 9)]
    [InlineData(7, 469, 6)]
    // Anything beyond the playfield ends up in the outermost column, like in-game.
    [InlineData(10, 511, 9)]
    [InlineData(10, 600, 9)]
    [InlineData(18, 497, 17)]
    public void ManiaTimestampUsesColumnOfAnyKeyCount(float keys, int x, int expectedColumn)
    {
        var beatmap = CreateManiaBeatmap(keys, $"{x},192,7740,1,0,0:0:0:0:");

        Assert.Equal($"00:07:740 (7740|{expectedColumn}) - ", Timestamp.Get(beatmap.HitObjects[0]));
    }

    [Fact]
    public void ManiaTimestampListsEveryObject()
    {
        var beatmap = CreateManiaBeatmap(
            10,
            "25,192,7740,1,0,0:0:0:0:",
            "486,192,7740,1,0,0:0:0:0:"
        );

        Assert.Equal(
            "00:07:740 (7740|0,7740|9) - ",
            Timestamp.Get(beatmap.HitObjects[0], beatmap.HitObjects[1])
        );
    }

    private static Beatmap CreateManiaBeatmap(float keys, params string[] hitObjects)
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "MapsetVerifierTests",
            Guid.NewGuid().ToString()
        );
        Directory.CreateDirectory(tempRoot);

        const string mapPath = "Test.osu";
        var osuCode = string.Join(
            "\n",
            new[]
            {
                "osu file format v14",
                "[General]",
                "Mode: 3",
                "[Metadata]",
                "Title:Title",
                "Artist:Artist",
                "Creator:Creator",
                "Version:Diff",
                "[Difficulty]",
                "HPDrainRate:5",
                $"CircleSize:{keys.ToString(CultureInfo.InvariantCulture)}",
                "OverallDifficulty:8",
                "ApproachRate:5",
                "SliderMultiplier:1.4",
                "SliderTickRate:1",
                "[TimingPoints]",
                "0,250,4,2,1,50,1,0",
                "[HitObjects]",
            }.Concat(hitObjects)
        );

        File.WriteAllText(Path.Combine(tempRoot, mapPath), osuCode);

        return new Beatmap(osuCode, tempRoot, mapPath);
    }
}
