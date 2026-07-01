using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

public class BeatmapTests
{
    private Beatmap CreateBeatmap(string version, Beatmap.Mode mode = Beatmap.Mode.Standard)
    {
        var code = $"""
[General]
Mode: {(int)mode}

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

    [Fact]
    public void TaikoOni_PromotesByStarRating_WhenItIsTopDifficulty()
    {
        using var beatmapSet = CreateBeatmapSet([
            ("oni.osu", BuildTaikoOsu("Oni", BuildDenseTaikoObjects(90, 180))),
        ]);

        var oni = GetBeatmap(beatmapSet, "Oni");

        Assert.True(
            oni.StarRating >= 5.3,
            $"Expected generated Oni to be Expert SR, got {oni.StarRating}."
        );
        Assert.Equal(Beatmap.Difficulty.Expert, oni.GetDifficulty());
    }

    [Fact]
    public void TaikoOni_RemainsInsane_WhenInnerOniExists()
    {
        using var beatmapSet = CreateBeatmapSet([
            ("oni.osu", BuildTaikoOsu("Oni", BuildDenseTaikoObjects(90, 180))),
            ("inner-oni.osu", BuildTaikoOsu("Inner Oni", BuildDenseTaikoObjects(90, 180))),
        ]);

        var oni = GetBeatmap(beatmapSet, "Oni");
        var innerOni = GetBeatmap(beatmapSet, "Inner Oni");

        Assert.True(
            oni.StarRating >= 5.3,
            $"Expected generated Oni to be Expert SR, got {oni.StarRating}."
        );
        Assert.Equal(Beatmap.Difficulty.Insane, oni.GetDifficulty());
        Assert.Equal(Beatmap.Difficulty.Expert, innerOni.GetDifficulty());
    }

    [Fact]
    public void TaikoOni_RemainsInsane_WhenHigherStarRatingDifficultyExists()
    {
        using var beatmapSet = CreateBeatmapSet([
            ("oni.osu", BuildTaikoOsu("Oni", BuildDenseTaikoObjects(90, 180))),
            ("extra.osu", BuildTaikoOsu("Custom", BuildDenseTaikoObjects(60, 220))),
        ]);

        var oni = GetBeatmap(beatmapSet, "Oni");
        var custom = GetBeatmap(beatmapSet, "Custom");

        Assert.True(
            oni.StarRating >= 5.3,
            $"Expected generated Oni to be Expert SR, got {oni.StarRating}."
        );
        Assert.True(custom.StarRating > oni.StarRating);
        Assert.Equal(Beatmap.Difficulty.Insane, oni.GetDifficulty());
    }

    private sealed class TemporaryBeatmapSet : IDisposable
    {
        public TemporaryBeatmapSet(string path, BeatmapSet beatmapSet)
        {
            Path = path;
            BeatmapSet = beatmapSet;
        }

        public string Path { get; }
        public BeatmapSet BeatmapSet { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static TemporaryBeatmapSet CreateBeatmapSet(
        IEnumerable<(string FileName, string Content)> osuFiles
    )
    {
        var tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "MapsetVerifierParserTests",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(tempPath);

        foreach (var (fileName, content) in osuFiles)
            File.WriteAllText(System.IO.Path.Combine(tempPath, fileName), content);

        return new TemporaryBeatmapSet(tempPath, new BeatmapSet(tempPath));
    }

    private static Beatmap GetBeatmap(TemporaryBeatmapSet beatmapSet, string difficultyName) =>
        beatmapSet.BeatmapSet.Beatmaps.Single(beatmap =>
            beatmap.MetadataSettings.version == difficultyName
        );

    private static IEnumerable<string> BuildDenseTaikoObjects(int spacingMs, int count) =>
        Enumerable.Range(0, count).Select(i => $"256,192,{1000 + i * spacingMs},1,0,0:0:0:0:");

    private static string BuildTaikoOsu(string version, IEnumerable<string> hitObjects)
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename:",
            $"Mode: {(int)Beatmap.Mode.Taiko}",
            "[Metadata]",
            "Title:Taiko Difficulty Test",
            "Artist:MapsetVerifier",
            "Creator:Tests",
            $"Version:{version}",
            "[Difficulty]",
            "CircleSize:4",
            "HPDrainRate:5",
            "OverallDifficulty:5",
            "ApproachRate:5",
            "SliderMultiplier:1.4",
            "SliderTickRate:1",
            "[Events]",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
        };
        lines.AddRange(hitObjects);
        return string.Join("\n", lines);
    }
}
