using System.Globalization;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects.Catch;

/// <summary>
/// Builds and loads osu!catch beatmaps for movement tests. Distances are calculated while parsing,
/// so the hit objects of the returned beatmap are already classified.
/// </summary>
internal static class CatchTestBeatmap
{
    /// <summary>A circle at the given time and x position, using the wiki field order.</summary>
    public static string Circle(int time, int x) => $"{x},192,{time},1,0,0:0:0:0:";

    /// <summary>A spinner between the given times.</summary>
    public static string Spinner(int time, int endTime) => $"256,192,{time},8,0,{endTime},0:0:0:0:";

    /// <summary>Parses a catch beatmap containing the given hit object lines.</summary>
    public static Beatmap Create(float circleSize, params string[] hitObjects)
    {
        var code = $"""
            osu file format v14

            [General]
            Mode: 2

            [Metadata]
            Title:Test
            Artist:Tests
            Creator:Tests
            Version:Test

            [Difficulty]
            CircleSize:{circleSize.ToString(CultureInfo.InvariantCulture)}
            HPDrainRate:5
            OverallDifficulty:5
            ApproachRate:5
            SliderMultiplier:1.4
            SliderTickRate:1

            [TimingPoints]
            0,500,4,2,0,100,1,0

            [HitObjects]
            {string.Join("\n", hitObjects)}
            """;

        return new Beatmap(code, "song", "map.osu");
    }

    /// <summary>
    /// Loads a difficulty from a fixture folder under <c>TestData/Beatmaps</c>. The raw file is used
    /// rather than a beatmap set so the bookmarks, which are not parsed into the model, stay readable.
    /// </summary>
    public static Beatmap Load(string fixture, string version)
    {
        var path = FindFile(fixture, version);

        return new Beatmap(File.ReadAllText(path), "song", path);
    }

    /// <summary>Bookmarks are not parsed into the beatmap model, so read them from the file.</summary>
    public static List<double> ReadBookmarks(string fixture, string version) =>
        File.ReadLines(FindFile(fixture, version))
            .First(line => line.StartsWith("Bookmarks:", StringComparison.Ordinal))[
                "Bookmarks:".Length..
            ]
            .Split(',')
            .Select(value => double.Parse(value.Trim(), CultureInfo.InvariantCulture))
            .ToList();

    private static string FindFile(string fixture, string version)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "TestData", "Beatmaps", fixture);

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(
                $"Fixture '{fixture}' was not found at '{directory}'."
            );

        return Directory
            .EnumerateFiles(directory, "*.osu")
            .First(path =>
                Path.GetFileName(path).Contains($"[{version}]", StringComparison.Ordinal)
            );
    }
}
