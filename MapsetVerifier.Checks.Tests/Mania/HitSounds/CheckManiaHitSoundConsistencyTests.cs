using MapsetVerifier.Checks.Mania.HitSounds;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.HitSounds;

public class CheckManiaHitSoundConsistencyTests
{
    [Fact]
    public void IdenticalHitSounding_IsNotReported()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("easy.osu", ManiaOsu.Build("Easy", hitObjects: Clapped())),
            ("hard.osu", ManiaOsu.Build("Hard", hitObjects: Clapped())),
        ]);

        Assert.Empty(context.RunBeatmapSetCheck<CheckManiaHitSoundConsistency>());
    }

    /// <summary> A note without an explicit hit normal sounds the same as one with, so neither is missing anything. </summary>
    [Fact]
    public void ImplicitAndExplicitHitNormal_AreNotReported()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("easy.osu", ManiaOsu.Build("Easy", hitObjects: [ManiaOsu.Note(1000)])),
            (
                "hard.osu",
                ManiaOsu.Build(
                    "Hard",
                    hitObjects: [ManiaOsu.Note(1000, HitObject.HitSounds.Normal)]
                )
            ),
        ]);

        Assert.Empty(context.RunBeatmapSetCheck<CheckManiaHitSoundConsistency>());
    }

    [Fact]
    public void AdditionMissingInOneDifficulty_IsReported()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("easy.osu", ManiaOsu.Build("Easy", hitObjects: [ManiaOsu.Note(1000)])),
            (
                "hard.osu",
                ManiaOsu.Build("Hard", hitObjects: [ManiaOsu.Note(1000, HitObject.HitSounds.Clap)])
            ),
        ]);

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckManiaHitSoundConsistency>());

        Assert.Equal("Easy", issue.beatmap!.MetadataSettings.version);
        Assert.Contains("normal-hitclap", issue.message);
        Assert.Contains("Hard", issue.message);
    }

    /// <summary> A difficulty can't play a sample where it has no note, so those times aren't compared. </summary>
    [Fact]
    public void NoteOnlyPresentInOneDifficulty_IsNotReported()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("easy.osu", ManiaOsu.Build("Easy", hitObjects: [ManiaOsu.Note(1000)])),
            (
                "hard.osu",
                ManiaOsu.Build(
                    "Hard",
                    hitObjects: [ManiaOsu.Note(1000), ManiaOsu.Note(1250, HitObject.HitSounds.Clap)]
                )
            ),
        ]);

        Assert.Empty(context.RunBeatmapSetCheck<CheckManiaHitSoundConsistency>());
    }

    [Fact]
    public void SingleDifficulty_IsNotReported()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("hard.osu", ManiaOsu.Build("Hard", hitObjects: Clapped())),
        ]);

        Assert.Empty(context.RunBeatmapSetCheck<CheckManiaHitSoundConsistency>());
    }

    [Fact]
    public void NonManiaDifficulties_AreNotCompared()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("mania.osu", ManiaOsu.Build("Hard", hitObjects: Clapped())),
            (
                "standard.osu",
                ManiaOsu
                    .Build("Standard", hitObjects: [ManiaOsu.Note(1000)])
                    .Replace(
                        $"Mode: {(int)Beatmap.Mode.Mania}",
                        $"Mode: {(int)Beatmap.Mode.Standard}"
                    )
            ),
        ]);

        Assert.Empty(context.RunBeatmapSetCheck<CheckManiaHitSoundConsistency>());
    }

    /// <summary>
    ///     A difficulty hit sounded entirely on its own is excluded rather than reported as missing every
    ///     sample the rest of the set has.
    /// </summary>
    [Fact]
    public void DifficultyWithItsOwnHitSounding_IsExcluded()
    {
        var shared = Clapped();
        var own = Enumerable
            .Range(0, 10)
            .Select(i =>
                ManiaOsu.Note(
                    1000 + i * 500,
                    HitObject.HitSounds.Whistle | HitObject.HitSounds.Finish
                )
            )
            .ToList();

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("easy.osu", ManiaOsu.Build("Easy", hitObjects: shared)),
            ("normal.osu", ManiaOsu.Build("Normal", hitObjects: shared)),
            ("hard.osu", ManiaOsu.Build("Hard", hitObjects: shared)),
            ("guest.osu", ManiaOsu.Build("Guest", hitObjects: own)),
        ]);

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckManiaHitSoundConsistency>());

        Assert.Equal("Guest", issue.beatmap!.MetadataSettings.version);
        Assert.Contains("its own hit sounding", issue.message);
    }

    private static List<string> Clapped() =>
        Enumerable
            .Range(0, 10)
            .Select(i => ManiaOsu.Note(1000 + i * 500, HitObject.HitSounds.Clap))
            .ToList();
}
