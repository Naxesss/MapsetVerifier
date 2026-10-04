using MapsetVerifier.Snapshots.Store;
using Xunit;
using static MapsetVerifier.Snapshots.Tests.StoreTestContext;

namespace MapsetVerifier.Snapshots.Tests;

public class SnapshotStoreTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Capture_StoresOnlyWhenSomethingChanged()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(4), beatmapId: 1));
        context.WriteFile("audio.mp3", "audio");

        var first = SnapshotStore.Capture(context.Load(), "checkRun", now: T0);
        var same = SnapshotStore.Capture(context.Load(), "checkRun", now: T0.AddMinutes(1));

        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(5), beatmapId: 1));
        var changed = SnapshotStore.Capture(context.Load(), "checkRun", now: T0.AddMinutes(2));

        Assert.NotNull(first);
        Assert.Null(same);
        Assert.NotNull(changed);
        Assert.Equal(2, SnapshotStore.Load("set-12345").Entries.Count);
    }

    [Fact]
    public void ChangedFile_CountsAsAChange_AndIdenticalContentIsStoredOnce()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(4), beatmapId: 1));
        context.WriteFile("audio.mp3", "one");
        SnapshotStore.Capture(context.Load(), "manual", now: T0);

        context.WriteFile("audio.mp3", "two");
        var changed = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(1));

        Assert.NotNull(changed);

        // Audio is only hashed, so the one difficulty file is the one blob.
        var blobs = Directory.GetFiles(
            Path.Combine(context.StoreRoot, "snapshots", "set-12345", "blobs"),
            "*.gz"
        );
        Assert.Single(blobs);
    }

    [Fact]
    public void PinOnAnUnchangedSet_LabelsTheLatestSnapshot()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(4), beatmapId: 1));
        SnapshotStore.Capture(context.Load(), "manual", now: T0);

        var pinned = SnapshotStore.Capture(
            context.Load(),
            "manual",
            "After mod round 2",
            T0.AddMinutes(1)
        );

        Assert.Equal("After mod round 2", pinned!.Pin);
        Assert.Single(SnapshotStore.Load("set-12345").Entries);
    }

    [Fact]
    public void OlderDuplicateOfTheSameBeatmapId_IsIgnored()
    {
        using var context = Create();
        context.WriteDifficulty(
            "old.osu",
            BuildOsu("Old copy", Circles(3), beatmapId: 7),
            DateTime.UtcNow.AddDays(-1)
        );
        context.WriteDifficulty(
            "new.osu",
            BuildOsu("New copy", Circles(4), beatmapId: 7),
            DateTime.UtcNow
        );

        var entry = SnapshotStore.Capture(context.Load(), "manual", now: T0);

        var difficulty = Assert.Single(entry!.Difficulties);
        Assert.Equal("New copy", difficulty.Version);
    }

    [Fact]
    public void UnsubmittedDifficulties_AreKeptApart_AndFollowTheSetOnceSubmitted()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), setId: null));
        context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(4), setId: null));

        var local = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;
        var localKey = SnapshotStore.ResolveSetKey(context.Load());

        Assert.StartsWith("local-", localKey);
        Assert.Equal(2, local.Difficulties.Select(d => d.Key).Distinct().Count());

        // The same folder gets its ids when the set is uploaded.
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 11, setId: 500));
        context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(4), beatmapId: 12, setId: 500));
        var submitted = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddHours(1))!;

        var log = SnapshotStore.Load("set-500");

        Assert.Equal(2, log.Entries.Count);
        Assert.Equal("Submitted", submitted.Pin);
        Assert.All(log.Entries, e => Assert.Contains(e.Difficulties, d => d.Key == "b-11"));
        Assert.False(Directory.Exists(Path.Combine(context.StoreRoot, "snapshots", localKey)));
    }

    [Fact]
    public void ReadBlob_ReturnsTheStoredCode()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        var entry = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;

        var stored = SnapshotStore.ReadBlob("set-12345", entry.Difficulties[0].Blob);

        Assert.Contains("Version:Easy", stored);
        Assert.Contains("[HitObjects]", stored);
    }
}
