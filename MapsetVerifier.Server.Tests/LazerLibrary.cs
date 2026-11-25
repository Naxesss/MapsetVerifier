using osu.Game.Beatmaps;
using osu.Game.Models;
using Realms;

namespace MapsetVerifier.Server.Tests;

/// <summary>
/// A throwaway osu!lazer data folder. Its database is written with lazer's own models, so it has the same
/// schema as a real one. Also keeps the lookup of a data folder away from the actual one.
/// </summary>
public sealed class LazerLibrary : IDisposable
{
    private readonly SongFolder folder = new();
    private readonly string? appData = Environment.GetEnvironmentVariable("APPDATA");
    private readonly RealmConfiguration configuration;

    public LazerLibrary()
    {
        AppDataPath = folder.Path;
        dataPath = System.IO.Path.Combine(folder.Path, "osu");
        Directory.CreateDirectory(DataPath);
        Environment.SetEnvironmentVariable("APPDATA", AppDataPath);

        configuration = new RealmConfiguration(System.IO.Path.Combine(DataPath, "client.realm"))
        {
            SchemaVersion = 51,
        };
        Write(_ => { });
    }

    public string AppDataPath { get; }

    /// <summary> Where the database is, which is needed to read it. </summary>
    public string DataPath
    {
        get
        {
            LeaveSynchronizationContext();
            return dataPath;
        }
    }

    private readonly string dataPath;

    /// <summary>
    /// Realm would post to the context xUnit sets for each test, after the database was closed, and fail. The server
    /// has none. xUnit brings back its own after the test.
    /// </summary>
    private static void LeaveSynchronizationContext() =>
        SynchronizationContext.SetSynchronizationContext(null);

    /// <summary> The folders this library's sets may have been copied to. </summary>
    public List<string> Materialized { get; } = [];

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("APPDATA", appData);

        foreach (var path in Materialized.Where(Directory.Exists))
            Directory.Delete(path, true);

        folder.Dispose();
    }

    /// <summary> Changes the database, which is closed again after so that it can be read. </summary>
    public void Write(Action<Realm> change)
    {
        LeaveSynchronizationContext();

        using (var realm = Realm.GetInstance(configuration))
            realm.Write(() => change(realm));

        // Told apart from before, however quickly it was changed again.
        File.SetLastWriteTimeUtc(configuration.DatabasePath, DateTime.UtcNow.AddSeconds(++writes));
    }

    private int writes;

    /// <summary> Where the contents of a file with the given hash are. </summary>
    public string FilePath(string hash) =>
        System.IO.Path.Combine(DataPath, "files", hash[..1], hash[..2], hash);

    public void AddFile(string hash, byte[] content)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath(hash))!);
        File.WriteAllBytes(FilePath(hash), content);
    }

    public void AddFile(string hash, string content) =>
        AddFile(hash, System.Text.Encoding.UTF8.GetBytes(content));

    public Guid AddSet(
        string title = "Title",
        string artist = "Artist",
        string? creator = "Creator",
        int onlineId = 0,
        int day = 1,
        int? editedDay = null,
        string[]? versions = null,
        string background = "",
        bool deleted = false,
        params (string Name, string Hash)[] files
    )
    {
        var id = Guid.NewGuid();
        Materialized.Add(
            System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MapsetVerifier",
                "LazerMaterialized",
                id.ToString()
            )
        );

        Write(realm =>
        {
            var ruleset =
                realm.Find<osu.Game.Rulesets.RulesetInfo>("osu")
                ?? realm.Add(new osu.Game.Rulesets.RulesetInfo("osu", "osu!", "", 0));
            var set = new BeatmapSetInfo
            {
                ID = id,
                OnlineID = onlineId,
                DateAdded = Date(day),
                DeletePending = deleted,
            };

            foreach (var version in versions ?? ["Easy"])
                set.Beatmaps.Add(
                    new BeatmapInfo(
                        ruleset,
                        new BeatmapDifficulty(),
                        new BeatmapMetadata(
                            creator == null ? null : new RealmUser { Username = creator }
                        )
                        {
                            Title = title,
                            Artist = artist,
                            BackgroundFile = background,
                        }
                    )
                    {
                        DifficultyName = version,
                        OnlineID = onlineId * 10 + set.Beatmaps.Count,
                        LastLocalUpdate = editedDay == null ? null : Date(editedDay.Value),
                        BeatmapSet = set,
                    }
                );

            foreach (var (name, hash) in files)
                set.Files.Add(
                    new RealmNamedFileUsage(
                        realm.Find<RealmFile>(hash) ?? realm.Add(new RealmFile { Hash = hash }),
                        name
                    )
                );

            realm.Add(set);
        });

        return id;
    }

    public static DateTimeOffset Date(int day) => new(2030, 1, day, 12, 0, 0, TimeSpan.Zero);
}
