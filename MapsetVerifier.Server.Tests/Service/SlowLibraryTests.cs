using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

/// <summary> Folders which take long to read, or can't be, must not leave the song list loading forever. </summary>
public sealed class SlowLibraryTests : IDisposable
{
    private readonly SongFolder songs = new();
    private readonly TimeSpan budget = BeatmapService.FolderScanWaitBudget;
    private readonly ManualResetEventSlim released = new(false);

    public SlowLibraryTests() =>
        BeatmapService.FolderScanWaitBudget = TimeSpan.FromMilliseconds(200);

    public void Dispose()
    {
        released.Set();
        BeatmapService.FolderScanWaitBudget = budget;
        BeatmapService.ReadLastEdit = BeatmapService.GetEffectiveLastWriteTimeUtc;
        songs.Dispose();
    }

    /// <summary> By its folder, the first is the newest. By its last edit, the last is. </summary>
    private void AddSets(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var file = songs.Write($"{i}/a.osu", SongFolder.Osu(title: $"{i}"));
            File.SetLastWriteTimeUtc(file, Time.AddMinutes(60 + i));
        }

        for (var i = 0; i < count; i++)
            Directory.SetLastWriteTimeUtc(Path.Combine(songs.Path, $"{i}"), Time.AddMinutes(-i));
    }

    private static readonly DateTime Time = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private void BlockOn(string folder) =>
        BeatmapService.ReadLastEdit = directory =>
        {
            if (directory.Name == folder)
                released.Wait(TimeSpan.FromSeconds(30));

            return BeatmapService.GetEffectiveLastWriteTimeUtc(directory);
        };

    private ApiBeatmapPage Get() =>
        BeatmapService.GetBeatmaps(songs.Path, null, 0, 16, new HashSet<string>());

    private static string[] Titles(ApiBeatmapPage page) =>
        page.Items.Select(item => item.Title).ToArray();

    [Fact]
    public void SlowFolder_IsNamed_AndTheListSortedByFolderUntilItIsRead()
    {
        AddSets(3);
        BlockOn("1");

        Assert.Equal(["0", "1", "2"], Titles(Get()));

        // Only those taking over a second are.
        Assert.Empty(Get().ScanWarning!.Value.SlowFolders);
        Thread.Sleep(1100);
        var page = Get();
        Assert.Equal("1", Assert.Single(page.ScanWarning!.Value.SlowFolders).Folder);
        Assert.True(page.ScanWarning.Value.SlowFolders.First().Seconds >= 1);

        released.Set();
        BeatmapService.FolderScanWaitBudget = TimeSpan.FromSeconds(30);
        page = Get();
        Assert.Equal(["2", "1", "0"], Titles(page));
        Assert.Null(page.ScanWarning);
    }

    [Fact]
    public void SlowFolder_KeepsTheLastOrderOfTheSameLibrary()
    {
        AddSets(3);
        BeatmapService.FolderScanWaitBudget = TimeSpan.FromSeconds(30);
        Assert.Equal(["2", "1", "0"], Titles(Get()));

        // Until it's worth looking again.
        Thread.Sleep(5200);
        BeatmapService.FolderScanWaitBudget = TimeSpan.FromMilliseconds(200);
        BlockOn("1");

        Assert.Equal(["2", "1", "0"], Titles(Get()));
        Assert.Null(Get().ScanWarning);
    }

    [Fact]
    public void FailedScan_SortsByFolder_AndIsTriedAgain()
    {
        AddSets(3);
        BeatmapService.FolderScanWaitBudget = TimeSpan.FromSeconds(30);
        BeatmapService.ReadLastEdit = _ => throw new IOException("broken");

        var page = Get();
        Assert.Equal(["0", "1", "2"], Titles(page));
        Assert.Empty(page.ScanWarning!.Value.SlowFolders);

        BeatmapService.ReadLastEdit = BeatmapService.GetEffectiveLastWriteTimeUtc;
        page = Get();
        Assert.Equal(["2", "1", "0"], Titles(page));
        Assert.Null(page.ScanWarning);
    }

    [Fact]
    public void LaterPages_ContinueTheListOfTheFirst()
    {
        AddSets(3);
        BeatmapService.FolderScanWaitBudget = TimeSpan.FromSeconds(30);

        ApiBeatmapPage Page(int page) =>
            BeatmapService.GetBeatmaps(songs.Path, null, page, 2, new HashSet<string>());

        Assert.Equal(["2", "1"], Titles(Page(0)));
        // A set edited while scrolling doesn't move what comes next.
        File.SetLastWriteTimeUtc(Path.Combine(songs.Path, "0", "a.osu"), Time.AddMinutes(500));
        Assert.Equal(["0"], Titles(Page(1)));
    }
}
