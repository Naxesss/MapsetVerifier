using MapsetVerifier.Server.Service.OsuRuntime;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

/// <summary> Which osu! is running and what's open in it, from what its process and window look like. </summary>
public sealed class OsuClientTests : IDisposable
{
    private readonly SongFolder songs = new();

    public void Dispose() => songs.Dispose();

    [Theory]
    [InlineData("osu!", @"C:\Users\a\AppData\Local\osu!\osu!.exe", null, true, false, "Stable")]
    [InlineData("osu!", @"C:\Games\osu!lazer\current\osu!.exe", null, true, false, "Lazer")]
    [InlineData("osu!", @"C:\Games\lazer\osu!.exe", null, true, false, "Lazer")]
    [InlineData("osu!", @"C:\Games\osu!\osu!.exe", null, true, true, "Lazer")] // By its libraries.
    [InlineData("osu!", null, null, true, false, "Unknown")]
    [InlineData("osu!", @"C:\Games\osu!\other.exe", null, true, false, "Unknown")]
    [InlineData("notepad", @"C:\Windows\notepad.exe", null, true, true, "Unknown")]
    [InlineData("MapsetVerifier", @"C:\osu!\MapsetVerifier.exe", null, true, true, "Unknown")]
    [InlineData("wine64", "/usr/bin/wine64", @"C:\osu!\osu!.exe", false, false, "Stable")]
    [InlineData("osu-wine", "/opt/osu-wine", null, false, false, "Stable")]
    [InlineData("yawl", "/a/yawl", "osu!.exe", false, false, "Stable")]
    [InlineData("pressure-vessel", "/a", "osu!.exe", false, false, "Stable")]
    [InlineData("wineskin", "/a", "osu!.exe", false, false, "Stable")]
    [InlineData("osu!", "/home/a/osu.AppImage", null, false, false, "Lazer")]
    [InlineData("osu!", "/tmp/.mount_osuAbc/usr/bin/osu!", null, false, false, "Lazer")]
    [InlineData("osu!", "/app/sh.ppy.osu/osu!", null, false, false, "Lazer")]
    [InlineData("osu!", "/Applications/osu!.app/osu!", null, false, false, "Lazer")]
    [InlineData("osu-lazer", "/usr/bin/osu-lazer", null, false, false, "Lazer")]
    [InlineData("osu!.lazer", "/usr/bin/a", null, false, false, "Lazer")]
    [InlineData("osulazer", "/usr/bin/a", null, false, false, "Lazer")]
    [InlineData("game", "/usr/bin/osu!", null, false, false, "Lazer")] // Only lazer runs natively.
    [InlineData("game", "/usr/bin/osu!tool", null, false, false, "Unknown")]
    public void Client_IsKnownByItsProcess(
        string name,
        string? path,
        string? commandLine,
        bool isWindows,
        bool hasLazerModules,
        string expected
    )
    {
        Assert.Equal(
            expected,
            OsuProcessClassifier
                .Classify(name, path, commandLine, isWindows, () => hasLazerModules)
                .ToString()
        );
    }

    [Theory]
    [InlineData("osu!  - A - B (C) [D].osu", "A - B (C) [D].osu")]
    [InlineData("osu!cuttingedge b20260101 - A - B (C) [D].osu", "A - B (C) [D].osu")]
    [InlineData("osu!stable - A - B (C) [D].osu", "A - B (C) [D].osu")]
    [InlineData("  A - B (C) [D].osu  ", "A - B (C) [D].osu")]
    [InlineData("A - B (C) [D].osu (modified)", "A - B (C) [D].osu")]
    [InlineData("a.osu | b.osu", "b.osu")]
    [InlineData("osu!", null)]
    [InlineData("osu!  - A - B", null)]
    [InlineData("", null)]
    public void EditorTitle_NamesTheMap(string title, string? expected)
    {
        Assert.Equal(expected, CurrentBeatmapLookupService.TryExtractEditorMetadata(title));
    }

    private const string Map = "A - B (C) [D].osu";

    private string Add(string folder, int minute, string content, string file = Map)
    {
        var path = songs.Write($"{folder}/{file}", content);
        File.SetLastWriteTimeUtc(path, new DateTime(2030, 1, 1, 12, minute, 0, DateTimeKind.Utc));

        return Path.GetDirectoryName(path)!;
    }

    [Fact]
    public void OpenMap_IsTheLastEditedOfThatName()
    {
        Add("old", 1, SongFolder.Osu());
        var path = Add(
            "new",
            5,
            SongFolder.Osu(
                title: "B",
                artist: "A",
                creator: "C",
                beatmapId: 12,
                beatmapSetId: 34,
                events: ["0,0,\"bg.jpg\",0,0"]
            )
        );
        Add("other", 9, SongFolder.Osu(), "other.osu");

        var result = CurrentBeatmapLookupService.ResolveStableCurrentMap(
            songs.Path,
            ["missing.osu", "osu! - " + Map]
        );

        Assert.Equal("folder_found", result.Status);
        Assert.Equal(path, result.FolderPath);
        Assert.Equal(songs.Path, result.LookupRoot);

        var beatmap = result.Beatmap!.Value;
        Assert.Equal(
            ("new", "B", "A", "C"),
            (beatmap.Folder, beatmap.Title, beatmap.Artist, beatmap.Creator)
        );
        Assert.Equal(("12", "34"), (beatmap.BeatmapId, beatmap.BeatmapSetId));
        Assert.Equal("/beatmap/image?folder=new", beatmap.BackgroundPath);
    }

    [Fact]
    public void OpenMap_StaysForAMoment_OnceTheTitleIsGone()
    {
        Add("set", 1, "no metadata");
        CurrentBeatmapLookupService.ResolveStableCurrentMap(songs.Path, [Map]);

        var result = CurrentBeatmapLookupService.ResolveStableCurrentMap(songs.Path, []);
        Assert.Equal("folder_found", result.Status);
        Assert.Contains("sticky", result.Message);
        Assert.Equal("", result.Beatmap!.Value.Title);
        Assert.Equal("", result.Beatmap.Value.BackgroundPath);

        Directory.Delete(Path.Combine(songs.Path, "set"), true);
        result = CurrentBeatmapLookupService.ResolveStableCurrentMap(songs.Path, []);
        Assert.Equal("metadata_detected", result.Status);
        Assert.Contains("last detected", result.Message);

        result = CurrentBeatmapLookupService.ResolveStableCurrentMap(songs.Path, [Map]);
        Assert.Equal("metadata_detected", result.Status);
        Assert.Contains("exact .osu filename", result.Message);

        // And the next one replaces it at once.
        Assert.Equal(
            "other.osu",
            CurrentBeatmapLookupService.ResolveStickyMetadata(OsuClientKind.Stable, "other.osu")
        );
        Thread.Sleep(4500);
        Assert.Equal(
            "no_editor_title",
            CurrentBeatmapLookupService.ResolveStableCurrentMap(songs.Path, []).Status
        );
    }

    [Fact]
    public void OpenMap_IsOfItsOwnClient()
    {
        CurrentBeatmapLookupService.ResolveStickyMetadata(OsuClientKind.Lazer, "lazer.osu");

        Assert.NotEqual(
            "lazer.osu",
            CurrentBeatmapLookupService.ResolveStickyMetadata(OsuClientKind.Stable, null)
        );
        Assert.Equal(
            "lazer.osu",
            CurrentBeatmapLookupService.ResolveStickyMetadata(OsuClientKind.Lazer, " ")
        );
        // Told apart by letters alone.
        Assert.Equal(
            "lazer.osu",
            CurrentBeatmapLookupService.ResolveStickyMetadata(OsuClientKind.Lazer, "Lazer!.osu")
        );
    }

    [Fact]
    public void OpenMap_WithoutASongsFolder_IsNotFound()
    {
        Assert.Equal(
            "metadata_detected",
            CurrentBeatmapLookupService
                .ResolveStableCurrentMap(Path.Combine(songs.Path, "missing"), [Map])
                .Status
        );
    }
}
