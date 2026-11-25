using MapsetVerifier.Snapshots.Store;
using Xunit;
using static MapsetVerifier.Snapshots.Tests.StoreTestContext;

namespace MapsetVerifier.Snapshots.Tests;

public class LegacyEdgeTests
{
    private static void Write(string snapshots, string folder, string file, string content)
    {
        var directory = Path.Combine(snapshots, "999", folder);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, file), content);
    }

    [Fact]
    public void SetWithoutUsableSnapshots_IsMovedAwayWithoutALog()
    {
        using var context = Create(snapshots =>
        {
            Write(snapshots, "11", "not a time.osu", BuildOsu("Easy", Circles(3), 11, 999));
            Write(snapshots, "12", "2020-01-01 10-00-00.txt", "Not a difficulty.");
        });

        Assert.Empty(SnapshotStore.Load("set-999").Entries);
        Assert.False(
            File.Exists(Path.Combine(context.StoreRoot, "snapshots", "set-999", "log.json"))
        );
        Assert.True(Directory.Exists(Path.Combine(context.StoreRoot, "snapshots-legacy", "999")));
    }

    [Fact]
    public void UnsubmittedAndNamelessDifficulties_AreKept()
    {
        using var context = Create(snapshots =>
        {
            Write(
                snapshots,
                "unsubmitted-Hard",
                "2020-01-01 10-00-00.osu",
                BuildOsu("Hard", Circles(3), null, 999).Replace("Mode: 0", "Mode: 2")
            );
            // Cut off halfway.
            Write(snapshots, "12", "2020-01-01 10-00-00.osu", "osu file format v14\nMode:7");
        });

        var difficulties = Assert.Single(SnapshotStore.Load("set-999").Entries).Difficulties;

        var hard = difficulties.Single(d => d.Key == "u-Hard");
        Assert.Null(hard.BeatmapId);
        Assert.Equal("Catch", hard.Mode);

        var nameless = difficulties.Single(d => d.Key == "b-12");
        Assert.Equal("12", nameless.Version);
        Assert.Equal("Mania", nameless.Mode);
    }

    [Fact]
    public void EmptyListing_IsNoSnapshotOfItsOwn()
    {
        using var context = Create(snapshots =>
        {
            Write(snapshots, "files", "2019-01-01 10-00-00.txt", "[Files]\n");
            Write(
                snapshots,
                "files",
                "2021-01-01 10-00-00.txt",
                "[Files]\nno separator\na.mp3: AB"
            );
            Write(
                snapshots,
                "11",
                "2020-01-01 10-00-00.osu",
                BuildOsu("Easy", Circles(3), 11, 999)
            );
        });

        var entries = SnapshotStore.Load("set-999").Entries;

        Assert.Equal([2020, 2021], entries.Select(e => e.Time.Year));
        Assert.Empty(entries[0].Files);
        Assert.Equal("a.mp3", Assert.Single(entries[1].Files).Key);
    }

    [Fact]
    public void ReplacedFile_IsASnapshotOfItsOwn()
    {
        using var context = Create(snapshots =>
        {
            Write(snapshots, "files", "2020-01-01 10-00-00.txt", "[Files]\na.mp3: AB");
            Write(snapshots, "files", "2020-01-02 10-00-00.txt", "[Files]\na.mp3: CD");
            Write(snapshots, "files", "2020-01-03 10-00-00.txt", "[Files]\nb.mp3: CD");
        });

        Assert.Equal(3, SnapshotStore.Load("set-999").Entries.Count);
    }

    [Fact]
    public void RebuildingTwice_KeepsPinsAndEveryEarlierCopy()
    {
        using var context = Create(snapshots =>
            Write(snapshots, "11", "2020-01-01 10-00-00.osu", BuildOsu("Easy", Circles(3), 11, 999))
        );
        var entry = Assert.Single(SnapshotStore.Load("set-999").Entries);
        SnapshotStore.SetPin("set-999", entry.Id, "Milestone");

        // A pinned one which the rebuilt history doesn't have.
        var logPath = Path.Combine(context.StoreRoot, "snapshots", "set-999", "log.json");
        var legacy = Path.Combine(context.StoreRoot, "snapshots-legacy", "999", "11");
        var indexPath = Path.Combine(context.StoreRoot, "snapshots", "index.json");

        // What an earlier rebuild left behind.
        Directory.CreateDirectory(Path.Combine(context.StoreRoot, "snapshots-repairing", "old"));
        Directory.CreateDirectory(Path.Combine(context.StoreRoot, "snapshots-superseded"));

        void Rebuild()
        {
            File.WriteAllText(
                indexPath,
                File.ReadAllText(indexPath).Replace("\"Migration\": 2", "\"Migration\": 0")
            );
            SnapshotStore.ConfigurePath(context.StoreRoot, "");
        }

        Rebuild();
        Assert.Equal("Milestone", Assert.Single(SnapshotStore.Load("set-999").Entries).Pin);
        Assert.Equal(
            2,
            Directory.GetDirectories(context.StoreRoot, "snapshots-superseded*").Length
        );
        Assert.True(File.Exists(logPath));

        // The old folder is gone, so there's nothing to rebuild it from.
        Directory.Delete(legacy, true);
        Directory.Delete(Path.Combine(context.StoreRoot, "snapshots-superseded"), true);
        Rebuild();
        Assert.Empty(SnapshotStore.Load("set-999").Entries);
    }

    [Fact]
    public void OldIndexWithoutOldSnapshots_IsOnlyMarkedAsDone()
    {
        using var context = Create(snapshots =>
        {
            Directory.CreateDirectory(snapshots);
            File.WriteAllText(Path.Combine(snapshots, "index.json"), "{\"Migration\": 0}");
        });

        Assert.Empty(SnapshotStore.Load("set-999").Entries);
        Assert.Contains(
            "\"Migration\": 2",
            File.ReadAllText(Path.Combine(context.StoreRoot, "snapshots", "index.json"))
        );
        Assert.False(Directory.Exists(Path.Combine(context.StoreRoot, "snapshots-superseded")));
    }
}
