using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Patterns;

public class UtilsTests
{
    [Theory]
    [InlineData(1, 2, 2)]
    [InlineData(-3, 2, -3)]
    [InlineData(2, -3, -3)]
    [InlineData(2, 2, 2)]
    public void HigherAbsoluteValue_KeepsItsSign(double first, double second, double expected)
    {
        Assert.Equal(expected, GeneralUtils.TakeHigherAbsValue(first, second));
    }

    [Theory]
    [InlineData("soft-hitnormal2.wav", true)]
    [InlineData("SOFT-HITNORMAL.WAV", true)]
    [InlineData("soft-hitclap.wav", false)]
    public void HitNormals_AreKnownByName(string name, bool expected)
    {
        Assert.Equal(expected, ManiaUtils.hasHitNormal(name));
    }

    [Fact]
    public void NoGaps_AreNotFast()
    {
        Assert.Equal(0, RhythmUtils.GetShareFasterThan([], 4));
    }

    [Fact]
    public void SetsWithoutDifficulties_HaveNothingToCompare()
    {
        var folder = Directory.CreateTempSubdirectory("MapsetVerifierChecksTests").FullName;

        try
        {
            var set = new BeatmapSet(folder);

            Assert.Empty(Run(new CheckInconsistentLines()));
            Assert.Empty(Run(new CheckPreview()));

            List<Issue> Run(BeatmapSetCheck check) => check.GetIssues(set).ToList();
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
