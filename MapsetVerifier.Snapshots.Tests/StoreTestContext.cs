using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots.Store;

namespace MapsetVerifier.Snapshots.Tests;

/// <summary>
/// A throwaway song folder and snapshot store. The store's root is process-wide state, so tests
/// using this context share one collection and run one after the other.
/// </summary>
public sealed class StoreTestContext : IDisposable
{
    private readonly string root;

    private StoreTestContext(string root, Action<string>? seedLegacy)
    {
        this.root = root;
        SongPath = Path.Combine(root, "song");
        StoreRoot = Path.Combine(root, "store");
        Directory.CreateDirectory(SongPath);
        Directory.CreateDirectory(StoreRoot);

        // The store starts migrating as soon as it is configured, so old data must already be there.
        seedLegacy?.Invoke(Path.Combine(StoreRoot, "snapshots"));
        SnapshotStore.ConfigurePath(StoreRoot, "");
    }

    public string SongPath { get; }

    /// <summary> The folder the store itself uses (the parent of "snapshots"). </summary>
    public string StoreRoot { get; }

    public static StoreTestContext Create(Action<string>? seedLegacy = null) =>
        new(
            Path.Combine(
                Path.GetTempPath(),
                "MapsetVerifierSnapshotsTests",
                Guid.NewGuid().ToString("N")
            ),
            seedLegacy
        );

    public static string BuildOsu(
        string version,
        string[] hitObjects,
        ulong? beatmapId = null,
        ulong? setId = 12345,
        string timing = "0,500,4,2,0,100,1,0",
        string extra = ""
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 0",
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:" + version,
            "Tags:alpha beta",
            "BeatmapID:" + (beatmapId?.ToString() ?? "0"),
            "BeatmapSetID:" + (setId?.ToString() ?? "-1"),
            "[Difficulty]",
            "CircleSize:4",
            "SliderMultiplier:1.4",
            extra,
            "[Events]",
            "[TimingPoints]",
        };
        lines.AddRange(timing.Split('|'));
        lines.Add("[HitObjects]");
        lines.AddRange(hitObjects);

        return string.Join("\n", lines);
    }

    public static string[] Circles(int count, int start = 1000, int step = 500, int x = 50) =>
        Enumerable
            .Range(0, count)
            .Select(i => $"{x + i * 20},100,{start + i * step},1,0,0:0:0:0:")
            .ToArray();

    public void WriteDifficulty(string fileName, string code, DateTime? created = null)
    {
        var path = Path.Combine(SongPath, fileName);
        File.WriteAllText(path, code);

        if (created != null)
            File.SetCreationTimeUtc(path, created.Value);
    }

    public void WriteFile(string fileName, string content = "") =>
        File.WriteAllText(Path.Combine(SongPath, fileName), content);

    public BeatmapSet Load() => new(SongPath);

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
