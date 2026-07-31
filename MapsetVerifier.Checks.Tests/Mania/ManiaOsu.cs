using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Tests.Mania;

/// <summary> Builds minimal mania <c>.osu</c> files for the mania checks to run against. </summary>
internal static class ManiaOsu
{
    /// <summary> Column centres of a 4 key mania beatmap, for placing notes into a chord. </summary>
    public const int Column1 = 64;
    public const int Column2 = 192;
    public const int Column3 = 320;
    public const int Column4 = 448;

    /// <summary> A timing line without a custom index, using the normal sampleset. </summary>
    public const string NormalTimingPoint = "0,500,4,1,0,100,1,0";

    public static string Build(
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
            "Title:Hit Sounds",
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

        lines.AddRange(timingPoints ?? [NormalTimingPoint]);
        lines.Add("[HitObjects]");
        lines.AddRange(hitObjects ?? [Note(1000)]);

        return string.Join("\n", lines);
    }

    /// <summary> A mania note, i.e. <c>x,y,time,type,hitSound,sampleset:addition:index:volume:filename</c>. </summary>
    public static string Note(
        double time,
        HitObject.HitSounds hitSound = HitObject.HitSounds.None,
        int column = Column1,
        int customIndex = 0,
        string fileName = ""
    ) => $"{column},192,{time},1,{(int)hitSound},0:0:{customIndex}:0:{fileName}";
}
