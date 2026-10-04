using MapsetVerifier.Snapshots.Store;
using Xunit;
using static MapsetVerifier.Snapshots.Tests.StoreTestContext;

namespace MapsetVerifier.Snapshots.Tests;

public class LegacyMigrationTests
{
    private const string FilesListing = "[Files]\r\naudio.mp3: ABCDEF\r\n";

    [Fact]
    public void OldSnapshots_BecomeLogEntries_AndTheOldFolderIsKept()
    {
        using var context = Create(snapshots =>
        {
            var easy = Path.Combine(snapshots, "999", "11");
            var files = Path.Combine(snapshots, "999", "files");
            Directory.CreateDirectory(easy);
            Directory.CreateDirectory(files);

            File.WriteAllText(
                Path.Combine(easy, "2020-01-01 10-00-00.osu"),
                BuildOsu("Easy", Circles(3), 11, 999)
            );
            File.WriteAllText(
                Path.Combine(easy, "2020-01-02 10-00-00.osu"),
                BuildOsu("Easy", Circles(4), 11, 999)
            );
            File.WriteAllText(Path.Combine(files, "2020-01-01 10-00-00.txt"), FilesListing);
            // The same files later: nothing changed, so it must not become a snapshot of its own.
            File.WriteAllText(Path.Combine(files, "2020-01-03 10-00-00.txt"), FilesListing);
        });

        var log = SnapshotStore.Load("set-999");

        Assert.Equal(2, log.Entries.Count);
        Assert.All(log.Entries, e => Assert.Equal("import", e.Trigger));
        Assert.Equal("b-11", log.Entries[0].Difficulties.Single().Key);
        Assert.Equal("Easy", log.Entries[0].Difficulties.Single().Version);
        Assert.Equal("abcdef", log.Entries[0].Files["audio.mp3"].Hash);
        Assert.True(
            Directory.Exists(Path.Combine(context.StoreRoot, "snapshots-legacy", "999", "11"))
        );
        Assert.Contains(
            "[HitObjects]",
            SnapshotStore.ReadBlob("set-999", log.Entries[1].Difficulties[0].Blob)
        );
    }

    [Fact]
    public void AnEmptyStore_NeedsNoMigration()
    {
        using var context = Create();

        Assert.Empty(SnapshotStore.Load("set-1").Entries);
        Assert.False(Directory.Exists(Path.Combine(context.StoreRoot, "snapshots-legacy")));
    }

    private static void WriteLegacy(string snapshots, string folder, string time, string code)
    {
        var directory = Path.Combine(snapshots, "999", folder);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, time + ".osu"), code);
    }

    private static void WriteListing(string snapshots, string time, params string[] names)
    {
        var directory = Path.Combine(snapshots, "999", "files");
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, time + ".txt"),
            "[Files]\r\n" + string.Join("", names.Select(n => n + ": ABC\r\n"))
        );
    }

    [Fact]
    public void ADifficultyThatLeftTheFilesListing_IsNotCarriedForwardForever()
    {
        using var context = Create(snapshots =>
        {
            WriteLegacy(
                snapshots,
                "11",
                "2020-01-01 10-00-00",
                BuildOsu("Easy", Circles(3), 11, 999)
            );
            WriteLegacy(
                snapshots,
                "12",
                "2020-01-01 10-00-00",
                BuildOsu("Gone", Circles(3), 12, 999)
            );
            WriteListing(snapshots, "2020-01-01 10-00-00", "A [Easy].osu", "A [Gone].osu");
            // Later "Gone" was deleted: the listing no longer names it, and Easy changed.
            WriteLegacy(
                snapshots,
                "11",
                "2020-02-01 10-00-00",
                BuildOsu("Easy", Circles(4), 11, 999)
            );
            WriteListing(snapshots, "2020-02-01 10-00-00", "A [Easy].osu");
        });

        var log = SnapshotStore.Load("set-999");

        Assert.Equal(2, log.Entries[0].Difficulties.Count);
        Assert.Equal("Easy", Assert.Single(log.Entries[1].Difficulties).Version);
    }

    [Fact]
    public void TwoFoldersForOneDifficultyName_KeepTheNewest()
    {
        using var context = Create(snapshots =>
        {
            // "Cup" was re-uploaded under a new beatmap id; the old folder is still there.
            WriteLegacy(
                snapshots,
                "31",
                "2020-01-01 10-00-00",
                BuildOsu("Cup", Circles(3), 31, 999)
            );
            WriteLegacy(
                snapshots,
                "32",
                "2020-03-01 10-00-00",
                BuildOsu("Cup", Circles(4), 32, 999)
            );
            WriteListing(snapshots, "2020-03-01 10-00-00", "A [Cup].osu");
        });

        var entry = SnapshotStore.Load("set-999").Entries.Last();

        Assert.Equal("b-32", Assert.Single(entry.Difficulties).Key);
    }

    [Fact]
    public void AStoreMigratedByTheFirstVersion_IsRebuilt_KeepingEverythingCapturedSince()
    {
        using var context = Create(snapshots =>
        {
            WriteLegacy(
                snapshots,
                "11",
                "2020-01-01 10-00-00",
                BuildOsu("Easy", Circles(3), 11, 999)
            );
            WriteLegacy(
                snapshots,
                "12",
                "2020-01-01 10-00-00",
                BuildOsu("Gone", Circles(3), 12, 999)
            );
            WriteListing(snapshots, "2020-01-01 10-00-00", "A [Easy].osu", "A [Gone].osu");
            WriteLegacy(
                snapshots,
                "11",
                "2020-02-01 10-00-00",
                BuildOsu("Easy", Circles(4), 11, 999)
            );
            WriteListing(snapshots, "2020-02-01 10-00-00", "A [Easy].osu");
        });

        // A real snapshot taken after the migration.
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(5), 11, 999));
        var captured = SnapshotStore.Capture(
            context.Load(),
            "pageOpen",
            now: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        )!;

        // Make it look like the store the first version produced: not yet repaired.
        var indexPath = Path.Combine(context.StoreRoot, "snapshots", "index.json");
        File.WriteAllText(
            indexPath,
            File.ReadAllText(indexPath).Replace("\"Migration\": 2", "\"Migration\": 0")
        );
        SnapshotStore.ConfigurePath(context.StoreRoot, "");

        var log = SnapshotStore.Load("set-999");

        Assert.Equal(3, log.Entries.Count);
        Assert.Equal(captured.Id, log.Entries[^1].Id);
        Assert.Equal("pageOpen", log.Entries[^1].Trigger);
        Assert.Equal("Easy", Assert.Single(log.Entries[1].Difficulties).Version);
        Assert.True(Directory.Exists(Path.Combine(context.StoreRoot, "snapshots-superseded")));
        Assert.Contains("\"Migration\": 2", File.ReadAllText(indexPath));
    }
}
