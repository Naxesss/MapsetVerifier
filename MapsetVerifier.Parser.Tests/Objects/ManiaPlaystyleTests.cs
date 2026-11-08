using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

public class ManiaPlaystyleTests
{
    [Theory]
    [InlineData(4, "[4K] Hard", "4K", "[4K]", "Hard", true)]
    [InlineData(7, "(7K) Hard", "7K", "(7K)", "Hard", true)]
    [InlineData(7, "7K - Hard", "7K", "7K", "Hard", true)]
    [InlineData(7, "Hard [7k]", "7K", "[7k]", "Hard", true)]
    // Guest difficulties, with the marker and its divider on either side of the mapper.
    [InlineData(7, "Mapper's [7K] Insane", "7K", "[7K]", "Mapper's Insane", true)]
    [InlineData(7, "[7K] Mapper's Insane", "7K", "[7K]", "Mapper's Insane", true)]
    [InlineData(7, "[7K] - Mapper's Insane", "7K", "[7K]", "Mapper's Insane", true)]
    [InlineData(7, "Mapper's [7K] - Insane", "7K", "[7K]", "Mapper's Insane", true)]
    [InlineData(7, "Sebas' 7K: Insane", "7K", "7K", "Sebas' Insane", true)]
    // The wrong marker is found too, but doesn't denote this one.
    [InlineData(8, "[7K] Mapper's Hard", "8K", "[7K]", "Mapper's Hard", false)]
    [InlineData(12, "[DP] Another", "12K", "[DP]", "Another", false)]
    [InlineData(7, "[7K+1] Hard", "7K", "[7K+1]", "Hard", false)]
    // No marker, or only part of a longer one.
    [InlineData(7, "Hard", "7K", null, null, false)]
    [InlineData(7, "Hard7K", "7K", null, null, false)]
    // Nothing left but the marker.
    [InlineData(7, "[7K]", "7K", "[7K]", "[7K]", true)]
    // N+1 only when the name says so; named after its key count, it's that key count.
    [InlineData(8, "[7K+1] Hard", "7K+1", "[7K+1]", "Hard", true)]
    [InlineData(8, "[7+1K] Hard", "7K+1", "[7+1K]", "Hard", true)]
    [InlineData(8, "8K (7+1) Hard", "7K+1", "8K (7+1)", "Hard", true)]
    [InlineData(8, "[8K] Hard", "8K", "[8K]", "Hard", true)]
    [InlineData(10, "10K // Hard", "10K", "10K", "Hard", true)]
    // Other ways of writing the key count.
    [InlineData(4, "4 Keys Hard", "4K", "4 Keys", "Hard", true)]
    [InlineData(4, "[4Key] Hard", "4K", "[4Key]", "Hard", true)]
    [InlineData(7, "Mapper's 7-key Insane", "7K", "7-key", "Mapper's Insane", true)]
    [InlineData(4, "4K // Beginner", "4K", "4K", "Beginner", true)]
    [InlineData(4, "4K | Mapper's Easy", "4K", "4K", "Mapper's Easy", true)]
    [InlineData(16, "[14K2S] Another", "16K", "[14K2S]", "Another", true)]
    [InlineData(7, "[4 Keys] Hard", "7K", "[4 Keys]", "Hard", false)]
    // Not key counts.
    [InlineData(4, "EXHAUST Lv.14", "4K", null, null, false)]
    [InlineData(4, "Monkeys", "4K", null, null, false)]
    // A playstyle written with its key count.
    [InlineData(16, "14K EZ2AC // Mapper's Hard", "EZ2AC", "14K EZ2AC", "Mapper's Hard", true)]
    [InlineData(16, "14K2S DP // CT's Hard", "DP", "14K2S DP", "CT's Hard", true)]
    [InlineData(16, "[DP 14K2S] Hard", "DP", "[DP 14K2S]", "Hard", true)]
    // Playstyles above 10K, only where they fit the key count.
    [InlineData(12, "[10K2S] Another", "10K2S", "[10K2S]", "Another", true)]
    [InlineData(16, "[DP] Mapper's Another", "DP", "[DP]", "Mapper's Another", true)]
    [InlineData(18, "[8K10K] Another", "10K8K", "[8K10K]", "Another", true)]
    [InlineData(14, "[14K] Another", "14K", "[14K]", "Another", true)]
    public void PlaystyleComesFromKeysAndName(
        float keys,
        string version,
        string label,
        string? marker,
        string? shortName,
        bool denoted
    )
    {
        var playstyle = ManiaPlaystyle.Of(Beatmap.Mode.Mania, keys, version);

        Assert.Equal(new ManiaPlaystyle(label, (int)keys, marker, shortName, denoted), playstyle);
    }

    [Fact]
    public void OtherModesHaveNone()
    {
        Assert.Null(ManiaPlaystyle.Of(Beatmap.Mode.Standard, 4, "[4K] Hard"));
    }
}
