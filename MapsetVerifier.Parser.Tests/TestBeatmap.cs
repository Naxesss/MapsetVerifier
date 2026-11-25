using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Parser.Tests;

/// <summary>Parses beatmaps from the sections a test cares about, with playable defaults for the rest.</summary>
internal static class TestBeatmap
{
    public const string DefaultTiming = "0,500,4,2,0,100,1,0";

    /// <summary>The sections of a format v14 beatmap as text, for tests which need the file rather than the object.</summary>
    public static string Code(
        string[]? hitObjects = null,
        string[]? timingPoints = null,
        string[]? events = null,
        string[]? colours = null,
        string general = "",
        string metadata = "Title:Title\nArtist:Artist\nCreator:Creator\nVersion:Test",
        string difficulty =
            "HPDrainRate:5\nCircleSize:4\nOverallDifficulty:5\nApproachRate:5\nSliderMultiplier:1.4\nSliderTickRate:1",
        int mode = 0
    ) =>
        string.Join(
            "\n",
            "osu file format v14",
            "",
            "[General]",
            $"Mode: {mode}",
            general,
            "",
            "[Metadata]",
            metadata,
            "",
            "[Difficulty]",
            difficulty,
            "",
            "[Events]",
            string.Join("\n", events ?? []),
            "",
            "[TimingPoints]",
            string.Join("\n", timingPoints ?? [DefaultTiming]),
            "",
            "[Colours]",
            string.Join("\n", colours ?? []),
            "",
            "[HitObjects]",
            string.Join("\n", hitObjects ?? [])
        );

    /// <summary>Uses a unique path, since timing line and hit object lookups are cached per path.</summary>
    public static Beatmap Parse(string code) => new(code, "song", $"{Guid.NewGuid():N}.osu");

    public static Beatmap WithObjects(params string[] hitObjects) => Parse(Code(hitObjects));

    public static Beatmap WithTiming(params string[] timingPoints) =>
        Parse(Code(timingPoints: timingPoints));

    public static string Circle(
        int time,
        int type = 1,
        int hitSound = 0,
        int x = 256,
        int y = 192
    ) => $"{x},{y},{time},{type},{hitSound},0:0:0:0:";
}
