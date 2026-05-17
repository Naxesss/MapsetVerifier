using MapsetVerifier.Checks.Mania.HitSounds;
using MapsetVerifier.Checks.Mania.Timing;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania;

public class ManiaCheckRegressionTests
{
    /// <summary>
    ///     Issue #100: <c>.osu</c> references <c>bell.wav</c> while the file lives in a subfolder; it still matches
    ///     <see cref="BeatmapSet.HitSoundFiles" />.
    /// </summary>
    [Fact]
    public void HitSoundInconsistencies_NoFalseMissingFile_WhenSampleInSubfolder()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("mania.osu", BuildManiaOsu(hitObjects: ["64,192,1000,1,0,0:0:0:0:bell.wav"]))],
            extraFiles: ["sounds/bell.wav"]
        );

        var issues = context.RunBeatmapSetCheck<CheckHitSoundInconsistencies>();

        Assert.DoesNotContain(
            issues,
            i => i.message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    ///     Issue #101: normalization green on the same offset as its red line must not trigger
    ///     "Normalized Value Moved Problem".
    /// </summary>
    [Fact]
    public void VariableBpm_NoNormalizedValueMoved_WhenGreenStackedOnRed()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "mania.osu",
                BuildManiaOsu(
                    version: "Hard",
                    timingPoints: ["0,500,4,2,0,100,1,0", "0,-100,4,2,0,100,0,0"]
                )
            ),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckVariableBpm>();

        Assert.DoesNotContain(
            issues,
            i =>
                i.message.Contains(
                    "Isn't on top of the previous uninherited timing line",
                    StringComparison.Ordinal
                )
        );
    }

    private static string BuildManiaOsu(
        string version = "Insane",
        IEnumerable<string>? timingPoints = null,
        IEnumerable<string>? hitObjects = null
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            $"Mode: {(int)Beatmap.Mode.Mania}",
            "[Metadata]",
            "Title:Regression",
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
        };

        lines.AddRange(timingPoints ?? ["0,500,4,2,0,100,1,0"]);
        lines.Add("[HitObjects]");
        lines.AddRange(hitObjects ?? ["64,192,1000,1,0,0:0:0:0:"]);

        return string.Join("\n", lines);
    }
}
