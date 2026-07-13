using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Snapshots.Tests;

/// <summary>
/// Builds a throwaway song folder plus a throwaway snapshot save path and points
/// <see cref="Snapshotter" /> at it for the duration of the test. Snapshotter's save path is
/// process-wide static state, so tests using this context must not run concurrently with each
/// other (they're kept in one collection/class so xUnit runs them sequentially).
/// </summary>
public sealed class SnapshotterTestContext : IDisposable
{
    private readonly string songPath;
    private readonly string snapshotRootPath;

    private SnapshotterTestContext(string songPath, string snapshotRootPath)
    {
        this.songPath = songPath;
        this.snapshotRootPath = snapshotRootPath;
    }

    public string SongPath => songPath;

    public static SnapshotterTestContext Create()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "MapsetVerifierSnapshotsTests",
            Guid.NewGuid().ToString("N")
        );
        var songPath = Path.Combine(root, "song");
        var snapshotRootPath = Path.Combine(root, "snapshot-store");

        Directory.CreateDirectory(songPath);
        Directory.CreateDirectory(snapshotRootPath);

        // ConfigurePath appends "snapshots" itself, so we hand it the parent as the "app data"
        // root and an empty external-folder-name so the final path is exactly snapshotRootPath\snapshots.
        Snapshotter.ConfigurePath(snapshotRootPath, "");

        return new SnapshotterTestContext(songPath, snapshotRootPath);
    }

    /// <summary>
    /// Writes (or overwrites) a difficulty's .osu file, optionally back-dating its creation time
    /// so tests can control which of several difficulties looks "newest" on disk.
    /// </summary>
    public string WriteDifficulty(string fileName, string code, DateTime? creationTimeUtc = null)
    {
        var path = Path.Combine(songPath, fileName);
        File.WriteAllText(path, code);

        if (creationTimeUtc != null)
            File.SetCreationTimeUtc(path, creationTimeUtc.Value);

        return path;
    }

    public void WriteExtraFile(string fileName, string content = "") =>
        File.WriteAllText(Path.Combine(songPath, fileName), content);

    public BeatmapSet LoadBeatmapSet() => new(songPath);

    public void Dispose()
    {
        TryDelete(songPath);
        TryDelete(snapshotRootPath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
