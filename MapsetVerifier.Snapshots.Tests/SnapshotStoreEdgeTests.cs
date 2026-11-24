using System.Text.RegularExpressions;
using MapsetVerifier.Snapshots.Compare;
using MapsetVerifier.Snapshots.Diffing;
using MapsetVerifier.Snapshots.Store;
using Xunit;
using static MapsetVerifier.Snapshots.Tests.StoreTestContext;

namespace MapsetVerifier.Snapshots.Tests;

public class SnapshotStoreEdgeTests
{
    private const string Set = "set-12345";
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static string LogPath(StoreTestContext context, string set = Set) =>
        Path.Combine(context.StoreRoot, "snapshots", set, "log.json");

    [Theory]
    [InlineData(5ul, "Easy", "b-5")]
    [InlineData(0ul, "Easy", "u-Easy")]
    [InlineData(null, "A/B", "u-A_B")]
    [InlineData(null, "", "u-unnamed")]
    public void DifficultyKey_IsTheIdOrTheName(ulong? id, string version, string expected)
    {
        Assert.Equal(expected, SnapshotStore.DifficultyKey(id, version));
    }

    [Fact]
    public void TwoCapturesInTheSameMillisecond_KeepTheirOrder()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        var first = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(4), beatmapId: 1));
        var second = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;

        Assert.True(string.CompareOrdinal(second.Id, first.Id) > 0);
        Assert.Equal(2, SnapshotStore.Load(Set).Entries.Count);
    }

    [Fact]
    public void RenamedFileAndReplacedDifficulty_AreChanges()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        context.WriteFile("one.mp3", "audio");
        SnapshotStore.Capture(context.Load(), "manual", now: T0);

        File.Move(
            Path.Combine(context.SongPath, "one.mp3"),
            Path.Combine(context.SongPath, "two.mp3")
        );
        Assert.NotNull(SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(1)));

        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 2));
        Assert.NotNull(SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(2)));
    }

    [Fact]
    public void Storyboard_IsKeptWhole()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        context.WriteFile("sb.osb", "[Events]\nSprite,Foreground,Centre,\"a.png\",320,240");
        var entry = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;

        Assert.Contains("a.png", SnapshotStore.ReadBlob(Set, entry.Files["sb.osb"].Hash));
    }

    [Fact]
    public void Pins_AreTrimmedClearedAndOnlyOnKnownSnapshots()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        var entry = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;

        Assert.True(SnapshotStore.SetPin(Set, entry.Id, " Milestone "));
        Assert.Equal("Milestone", SnapshotStore.Load(Set).Entries[0].Pin);
        Assert.True(SnapshotStore.SetPin(Set, entry.Id, "  "));
        Assert.Null(SnapshotStore.Load(Set).Entries[0].Pin);
        Assert.False(SnapshotStore.SetPin(Set, "1", "Milestone"));
    }

    [Fact]
    public void Checks_GoWithTheLatestSnapshot()
    {
        using var context = Create();

        // Nothing to go with yet.
        SnapshotStore.AttachChecks(Set, new StoredChecks());
        Assert.Empty(SnapshotStore.Load(Set).Entries);

        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        SnapshotStore.Capture(context.Load(), "manual", now: T0);
        SnapshotStore.AttachChecks(Set, new StoredChecks());
        Assert.NotNull(SnapshotStore.Load(Set).Entries[0].Checks);
    }

    [Fact]
    public void MissingStars_AreWorkedOutOnceAndSaved()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(30), beatmapId: 1));
        SnapshotStore.Capture(context.Load(), "manual", now: T0);

        // As an imported one.
        var log = SnapshotStore.Load(Set);
        File.WriteAllText(
            LogPath(context),
            Regex.Replace(
                File.ReadAllText(LogPath(context)),
                "\"Stars\": ?[0-9.]+",
                "\"Stars\": null"
            )
        );
        Assert.Null(SnapshotStore.Load(Set).Entries[0].Difficulties[0].Stars);

        var shown = SnapshotHistory.Build(Set).Difficulties.Single().StarRating;

        Assert.NotNull(shown);
        Assert.Equal(shown, SnapshotStore.Load(Set).Entries[0].Difficulties[0].Stars);

        // Known ones aren't replaced.
        SnapshotStore.SaveStars(
            Set,
            new Dictionary<string, double> { [log.Entries[0].Difficulties[0].Blob] = 99 }
        );
        Assert.Equal(shown, SnapshotStore.Load(Set).Entries[0].Difficulties[0].Stars);
        Assert.Null(SnapshotComparer.StarsOf(Set, null));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public void UnreadableLog_IsAnEmptyOne(string content)
    {
        using var context = Create();
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath(context))!);
        File.WriteAllText(LogPath(context), content);

        Assert.Empty(SnapshotStore.Load(Set).Entries);
        var history = SnapshotHistory.Build(Set);
        Assert.Empty(history.Entries);
        Assert.Empty(history.Difficulties);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public void UnreadableIndex_StillOpens(string content)
    {
        using var context = Create(snapshots =>
        {
            Directory.CreateDirectory(snapshots);
            File.WriteAllText(Path.Combine(snapshots, "index.json"), content);
        });
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));

        Assert.NotNull(SnapshotStore.Capture(context.Load(), "manual", now: T0));
    }

    [Fact]
    public void SubmittedSetWithoutItsLocalHistory_StartsOver()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), setId: null));
        SnapshotStore.Capture(context.Load(), "manual", now: T0);
        var local = SnapshotStore.ResolveSetKey(context.Load());
        Directory.Delete(Path.GetDirectoryName(LogPath(context, local))!, true);

        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 11, setId: 500));
        var entry = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddHours(1))!;

        Assert.Null(entry.Pin);
        Assert.Single(SnapshotStore.Load("set-500").Entries);
    }

    [Fact]
    public void SubmittedSet_MergesWithVersionsItAlreadyHas_AndKeepsDroppedDifficulties()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), setId: null));
        context.WriteDifficulty("b.osu", BuildOsu("Dropped", Circles(4), setId: null));
        SnapshotStore.Capture(context.Load(), "manual", now: T0);
        var local = SnapshotStore.ResolveSetKey(context.Load());
        var blob = SnapshotStore.Load(local).Entries[0].Difficulties[0].Blob;
        string Blob(string set) =>
            Path.Combine(Path.GetDirectoryName(LogPath(context, set))!, "blobs", blob + ".gz");
        Directory.CreateDirectory(Path.GetDirectoryName(Blob("set-500"))!);
        File.Copy(Blob(local), Blob("set-500"));

        File.Delete(Path.Combine(context.SongPath, "b.osu"));
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(5), beatmapId: 11, setId: 500));
        SnapshotStore.Capture(context.Load(), "manual", now: T0.AddHours(1));

        var merged = SnapshotStore.Load("set-500").Entries;

        Assert.Equal(2, merged.Count);
        Assert.Equal(["b-11", "u-Dropped"], merged[0].Difficulties.Select(d => d.Key).Order());
    }

    [Fact]
    public void LocalHistoryWithoutVersions_IsStillMerged()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), setId: null));
        SnapshotStore.Capture(context.Load(), "manual", now: T0);
        var local = SnapshotStore.ResolveSetKey(context.Load());
        Directory.Delete(Path.Combine(context.StoreRoot, "snapshots", local, "blobs"), true);

        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(5), beatmapId: 11, setId: 500));
        SnapshotStore.Capture(context.Load(), "manual", now: T0.AddHours(1));

        Assert.Equal(2, SnapshotStore.Load("set-500").Entries.Count);
    }

    [Fact]
    public void History_HasPlaystylesOfManiaOnly_AndSkipsChangesOfTheWholeSet()
    {
        using var context = Create();

        void Write(string tags)
        {
            context.WriteDifficulty(
                "a.osu",
                BuildOsu("4K Easy", ["64,192,1000,1,0,0:0:0:0:"], beatmapId: 1)
                    .Replace("Mode: 0", "Mode: 3")
                    .Replace("alpha beta", tags)
            );
            context.WriteDifficulty(
                "b.osu",
                BuildOsu("Hard", Circles(4), beatmapId: 2).Replace("alpha beta", tags)
            );
        }

        Write("alpha beta");
        SnapshotStore.Capture(context.Load(), "manual", now: T0);
        Write("alpha gamma");
        var after = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(1))!;

        var history = SnapshotHistory.Build(Set);

        Assert.Equal(1, history.Entries[0].GeneralChanges);
        Assert.Empty(history.Entries[0].ChangedDifficulties);
        Assert.NotNull(history.Difficulties.Single(d => d.Key == "b-1").Playstyle);
        Assert.Null(history.Difficulties.Single(d => d.Key == "b-2").Playstyle);

        // Its version is gone.
        File.Delete(
            Path.Combine(
                Path.GetDirectoryName(LogPath(context))!,
                "blobs",
                after.Difficulties.Single(d => d.Key == "b-1").Blob + ".gz"
            )
        );
        Assert.Null(SnapshotHistory.Build(Set).Difficulties.Single(d => d.Key == "b-1").Playstyle);
    }

    [Theory]
    [InlineData(ChangeOp.Added, 1, 0, 0)]
    [InlineData(ChangeOp.Removed, 0, 1, 0)]
    [InlineData(ChangeOp.Changed, 0, 0, 1)]
    public void Settings_AreCountedByWhatHappened(ChangeOp op, int added, int removed, int changed)
    {
        Assert.Equal(
            new ChangeCounts(added, removed, changed),
            SnapshotComparer.Count([], [new SettingChange("General", "Key", op)])
        );
    }
}
