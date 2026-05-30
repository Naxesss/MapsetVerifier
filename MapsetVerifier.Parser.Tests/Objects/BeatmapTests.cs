using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

public class BeatmapTests
{
    private Beatmap CreateBeatmap(string version, Beatmap.Mode mode = Beatmap.Mode.Standard)
    {
        var code =
$"""
[General]
Mode: {(int) mode}

[Metadata]
Version:{version}

[Difficulty]
HP:5
""";

        return new Beatmap(code, "song", "map.osu");
    }

    [Fact]
    public void DirectMatch_Kantan_ReturnsEasy()
    {
        var beatmap = CreateBeatmap("Kantan", Beatmap.Mode.Taiko);

        var result = beatmap.GetDifficultyFromName();

        Assert.Equal(Beatmap.Difficulty.Easy, result);
    }

    [Fact]
    public void DecoratedName_StillMatches_Kantan()
    {
        var beatmap = CreateBeatmap("~Kantan~", Beatmap.Mode.Taiko);

        var result = beatmap.GetDifficultyFromName();

        Assert.Equal(Beatmap.Difficulty.Easy, result);
    }

    [Fact]
    public void CollabPrefix_IsIgnored()
    {
        var beatmap = CreateBeatmap("Collab Kantan", Beatmap.Mode.Taiko);

        var result = beatmap.GetDifficultyFromName();

        Assert.Equal(Beatmap.Difficulty.Easy, result);
    }

    [Fact]
    public void OwnerPrefix_IsIgnored()
    {
        var beatmap = CreateBeatmap("Greaper's Kantan", Beatmap.Mode.Taiko);

        var result = beatmap.GetDifficultyFromName();

        Assert.Equal(Beatmap.Difficulty.Easy, result);
    }

    [Fact]
    public void InnerOni_IsCorrectlyMatched()
    {
        var beatmap = CreateBeatmap("Greaper's Inner Oni", Beatmap.Mode.Taiko);

        var result = beatmap.GetDifficultyFromName();

        Assert.Equal(Beatmap.Difficulty.Expert, result);
    }

    [Fact]
    public void NoiseDoesNotCreateFalsePositive()
    {
        var beatmap = CreateBeatmap("Nothing Comes Easy");

        var result = beatmap.GetDifficultyFromName();

        Assert.Null(result);
    }

    [Fact]
    public void EasySubstringDoesNotMatchInsideWord()
    {
        var beatmap = CreateBeatmap("NotAnEasyMap");

        var result = beatmap.GetDifficultyFromName();

        Assert.Null(result);
    }

    [Fact]
    public void CaseInsensitivity_Works()
    {
        var beatmap = CreateBeatmap("kAnTaN", Beatmap.Mode.Taiko);

        var result = beatmap.GetDifficultyFromName();

        Assert.Equal(Beatmap.Difficulty.Easy, result);
    }

    [Fact]
    public void ModeSpecificMapping_Taiko_Oni()
    {
        var beatmap = CreateBeatmap("Oni", Beatmap.Mode.Taiko);

        var result = beatmap.GetDifficultyFromName();

        Assert.Equal(Beatmap.Difficulty.Insane, result);
    }
}