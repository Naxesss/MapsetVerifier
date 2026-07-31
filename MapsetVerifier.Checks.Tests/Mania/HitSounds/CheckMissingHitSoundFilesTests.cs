using MapsetVerifier.Checks.Mania.HitSounds;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.HitSounds;

public class CheckMissingHitSoundFilesTests
{
    /// <summary>
    ///     Issue #100: additions without a custom index play the skin's samples, so the mapset isn't expected
    ///     to contain a file for them.
    /// </summary>
    [Fact]
    public void DefaultAdditions_AreNotReportedAsMissing()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "mania.osu",
                ManiaOsu.Build(
                    hitObjects:
                    [
                        ManiaOsu.Note(1000, HitObject.HitSounds.Whistle),
                        ManiaOsu.Note(1500, HitObject.HitSounds.Clap),
                        ManiaOsu.Note(2000, HitObject.HitSounds.Finish),
                    ]
                )
            ),
        ]);

        Assert.Empty(context.RunBeatmapSetCheck<CheckMissingHitSoundFiles>());
    }

    /// <summary> Issue #100: a referenced sample counts as present even when it lives in a subfolder. </summary>
    [Fact]
    public void ReferencedFileInSubfolder_IsNotReportedAsMissing()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "mania.osu",
                    ManiaOsu.Build(hitObjects: [ManiaOsu.Note(1000, fileName: "bell.wav")])
                ),
            ],
            extraFiles: ["sounds/bell.wav"]
        );

        Assert.Empty(context.RunBeatmapSetCheck<CheckMissingHitSoundFiles>());
    }

    [Fact]
    public void ReferencedFileNotInMapset_IsReportedAsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("mania.osu", ManiaOsu.Build(hitObjects: [ManiaOsu.Note(1000, fileName: "bell.wav")])),
        ]);

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckMissingHitSoundFiles>());

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("bell.wav", issue.message);
    }

    [Fact]
    public void IndexedSampleInMapset_IsNotReportedAsMissing()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "mania.osu",
                    ManiaOsu.Build(
                        timingPoints: ["0,500,4,1,2,100,1,0"],
                        hitObjects: [ManiaOsu.Note(1000, HitObject.HitSounds.Clap)]
                    )
                ),
            ],
            extraFiles: ["normal-hitclap2.wav", "normal-hitnormal2.wav"]
        );

        Assert.Empty(context.RunBeatmapSetCheck<CheckMissingHitSoundFiles>());
    }

    [Fact]
    public void IndexedSampleNotInMapset_IsReportedAsWarning()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "mania.osu",
                    ManiaOsu.Build(
                        timingPoints: ["0,500,4,1,2,100,1,0"],
                        hitObjects: [ManiaOsu.Note(1000, HitObject.HitSounds.Clap)]
                    )
                ),
            ],
            extraFiles: ["normal-hitnormal2.wav"]
        );

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckMissingHitSoundFiles>());

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("normal-hitclap2", issue.message);
    }

    /// <summary> The note's own custom index overrides the line's, so the line's index isn't what's looked up. </summary>
    [Fact]
    public void CustomIndexOnNote_OverridesTheLine()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "mania.osu",
                    ManiaOsu.Build(
                        timingPoints: ["0,500,4,1,5,100,1,0"],
                        hitObjects: [ManiaOsu.Note(1000, HitObject.HitSounds.Clap, customIndex: 2)]
                    )
                ),
            ],
            extraFiles: ["normal-hitclap2.wav", "normal-hitnormal2.wav"]
        );

        Assert.Empty(context.RunBeatmapSetCheck<CheckMissingHitSoundFiles>());
    }

    [Fact]
    public void MissingSample_IsReportedOncePerDifficulty()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "mania.osu",
                    ManiaOsu.Build(
                        timingPoints: ["0,500,4,1,2,100,1,0"],
                        hitObjects: Enumerable
                            .Range(0, 20)
                            .Select(i => ManiaOsu.Note(1000 + i * 500, HitObject.HitSounds.Clap))
                            .ToList()
                    )
                ),
            ],
            extraFiles: ["normal-hitnormal2.wav"]
        );

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckMissingHitSoundFiles>());

        Assert.Contains("normal-hitclap2", issue.message);
    }
}
