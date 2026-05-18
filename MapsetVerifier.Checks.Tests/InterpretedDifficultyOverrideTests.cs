using MapsetVerifier.Checks.Mania.Spread;
using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests;

public class InterpretedDifficultyOverrideTests
{
    /// <summary>
    ///     Four simultaneous notes violate Hard chord rules but not Insane rules; overriding interpretation to Hard
    ///     should surface <see cref="CheckSeven"/> only when the override is applied (via <see cref="Beatmap.GetDifficulty"/>).
    /// </summary>
    [Fact]
    public void MaxChordSize_UsesOverriddenDifficulty_ForThresholdSwitch()
    {
        var fillerNotes = Enumerable
            .Range(0, 48)
            .Select(i => $"64,192,{2000 + i * 80},1,0,0:0:0:0:")
            .ToList();
        var sameTimeChord = new[]
        {
            "64,192,1000,1,0,0:0:0:0:",
            "192,192,1000,1,0,0:0:0:0:",
            "320,192,1000,1,0,0:0:0:0:",
            "448,192,1000,1,0,0:0:0:0:",
        };
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "mania.osu",
                    BuildManiaOsu(
                        version: "Another",
                        timingPoints: ["0,500,4,2,0,100,1,0"],
                        hitObjects: sameTimeChord.Concat(fillerNotes)
                    )
                ),
            ],
            extraFiles: ["audio.mp3"]
        );

        var beatmap = context.GetBeatmap("Another");
        Assert.Equal(Beatmap.Difficulty.Insane, beatmap.GetDifficulty());

        var issuesNatural = new CheckSeven().GetIssues(beatmap).ToList();
        Assert.Empty(issuesNatural);

        beatmap.InterpretedDifficultyOverride = Beatmap.Difficulty.Hard;
        try
        {
            var issuesOverridden = new CheckSeven().GetIssues(beatmap).ToList();
            Assert.NotEmpty(issuesOverridden);
            Assert.Contains(
                issuesOverridden,
                i => i.message.Contains("Chords should not have more than", StringComparison.Ordinal)
            );
        }
        finally
        {
            beatmap.InterpretedDifficultyOverride = null;
        }
    }

    /// <summary>
    ///     <see cref="CheckCloseOverlap"/> only scans standard difficulties &lt;= Normal unless interpreted
    ///     difficulty jumps above Normal on the first map in sorted order.
    /// </summary>
    [Fact]
    public void CloseOverlap_SkipsEasyScan_WhenEasyMapInterpretedAsHard()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "easy.osu",
                    BuildStandardOsu(
                        version: "Easy",
                        hitObjects:
                        [
                            "100,100,0,1,0,0:0:0:0:",
                            "250,100,100,1,0,0:0:0:0:",
                        ]
                    )
                ),
                (
                    "normal.osu",
                    BuildStandardOsu(
                        version: "Normal",
                        hitObjects: ["300,100,0,1,0,0:0:0:0:"]
                    )
                ),
            ],
            extraFiles: ["a.mp3"]
        );

        var issuesNatural = context.RunBeatmapSetCheck<CheckCloseOverlap>();
        Assert.NotEmpty(issuesNatural);

        var easy = context.GetBeatmap("Easy");
        context.BeatmapSet.ApplyInterpretedDifficultyOverride(easy, Beatmap.Difficulty.Hard);
        var issuesOverridden = context.RunBeatmapSetCheck<CheckCloseOverlap>();
        Assert.Empty(issuesOverridden);
    }

    private static string BuildManiaOsu(
        string version,
        IEnumerable<string> timingPoints,
        IEnumerable<string> hitObjects
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            $"Mode: {(int)Beatmap.Mode.Mania}",
            "[Metadata]",
            "Title:OverrideTest",
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

        lines.AddRange(timingPoints);
        lines.Add("[HitObjects]");
        lines.AddRange(hitObjects);

        return string.Join("\n", lines);
    }

    private static string BuildStandardOsu(string version, IReadOnlyList<string> hitObjects)
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: a.mp3",
            $"Mode: {(int)Beatmap.Mode.Standard}",
            "[Metadata]",
            "Title:CloseOverlapOverride",
            "Artist:MapsetVerifier",
            "Creator:Tests",
            $"Version:{version}",
            "[Difficulty]",
            "HPDrainRate:5",
            "CircleSize:4",
            "OverallDifficulty:5",
            "ApproachRate:5",
            "SliderMultiplier:1.4",
            "SliderTickRate:1",
            "[Events]",
            "[TimingPoints]",
            "0,500,4,2,1,100,0,0",
            "[HitObjects]",
        };
        lines.AddRange(hitObjects);
        return string.Join("\n", lines);
    }
}
