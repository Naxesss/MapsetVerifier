using MapsetVerifier.Server.Service;
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
        // However little is different.
        Assert.Equal(
            "Lazer!.osu",
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

    [Theory]
    [InlineData("osu!", true)]
    [InlineData("OSU!", true)]
    [InlineData("wineserver", true)]
    [InlineData("yawl", true)]
    [InlineData("AppRun", true)]
    [InlineData("flatpak", true)]
    [InlineData("bwrap", true)]
    [InlineData("pressure-vessel", true)]
    [InlineData("notepad", false)]
    public void OtherProcesses_AreLeftAlone(string name, bool expected)
    {
        Assert.Equal(expected, OsuProcessClassifier.MayBeOsuRelated(name));
    }

    [Theory]
    [InlineData("osu.Game.dll", true)]
    [InlineData("osu.Framework.dll", true)]
    [InlineData("coreclr.dll", true)]
    [InlineData("HOSTFXR.DLL", true)]
    [InlineData("clr.dll", false)] // What stable runs on.
    [InlineData("", false)]
    public void Lazer_IsKnownByItsLibraries(string name, bool expected)
    {
        Assert.Equal(expected, OsuProcessClassifier.IsLazerModule(name));
    }

    [Fact]
    public void RunningProcess_IsReadable()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();

        Assert.Equal(OsuClientKind.Unknown, OsuProcessClassifier.Classify(process));
        // Runs on the same .NET as lazer does.
        Assert.Equal(
            OperatingSystem.IsWindows(),
            OsuProcessClassifier.IsLikelyLazerByModules(process)
        );
    }

    [Fact]
    public void ClosedProcess_IsNotReadable()
    {
        using var process = new System.Diagnostics.Process();

        Assert.False(OsuProcessClassifier.IsLikelyLazerByModules(process));
    }

    private static CurrentBeatmapLookupService.OsuProcessSnapshot Process(
        OsuClientKind kind,
        string? path = null,
        int id = 1
    ) => new(id, kind, path, null);

    /// <summary> Titles are given by process, as those of other windows aren't asked for. </summary>
    private static Model.ApiLazerLookupResult Find(
        OsuClientKind kind,
        string? path,
        CurrentBeatmapLookupService.OsuProcessSnapshot[] processes,
        params (int Process, string Title)[] titles
    ) =>
        CurrentBeatmapLookupService.GetCurrentBeatmap(
            kind,
            path,
            processes,
            ids =>
                titles
                    .Where(title => ids.Contains(title.Process))
                    .Select(title => new CurrentBeatmapLookupService.OsuWindowTitleCandidate(
                        title.Process,
                        title.Title
                    ))
                    .ToList()
        );

    private static void WaitUntilForgotten(OsuClientKind kind)
    {
        while (CurrentBeatmapLookupService.ResolveStickyMetadata(kind, null) != null)
            Thread.Sleep(100);
    }

    [Fact]
    public void ClosedClient_IsNotLookedInto()
    {
        Assert.Equal("no_process", Find(OsuClientKind.Lazer, null, []).Status);
        Assert.Equal("no_process", Find(OsuClientKind.Stable, null, []).Status);

        var result = Find(OsuClientKind.Lazer, null, [Process(OsuClientKind.Stable)]);
        Assert.Equal("no_process", result.Status);
        Assert.Contains("osu!lazer", result.Message);

        result = Find(OsuClientKind.Lazer, null, [Process(OsuClientKind.Unknown)]);
        Assert.Equal("ambiguous_client", result.Status);
    }

    [Fact]
    public void OpenMap_OfLazer_IsThatOfItsWindow()
    {
        WaitUntilForgotten(OsuClientKind.Lazer);
        using var library = new LazerLibrary();
        library.AddFile("aa1", SongFolder.Osu());
        var id = library.AddSet(title: "B", artist: "A", versions: ["D"], files: ("a.osu", "aa1"));

        CurrentBeatmapLookupService.OsuProcessSnapshot[] processes =
        [
            Process(OsuClientKind.Stable, id: 1),
            Process(OsuClientKind.Lazer, id: 2),
        ];

        Assert.Equal(
            "no_editor_title",
            Find(
                OsuClientKind.Lazer,
                library.DataPath,
                processes,
                (1, "osu! - A - Other (C) [D].osu"),
                (2, "osu!")
            ).Status
        );

        var result = Find(
            OsuClientKind.Lazer,
            library.DataPath,
            processes,
            (2, "osu! - A - B (Creator) [D].osu")
        );
        Assert.Equal("folder_found", result.Status);
        Assert.Equal(id.ToString(), result.Beatmap!.Value.Folder);
    }

    [Fact]
    public void OpenMap_OfStable_IsThatOfItsFolder()
    {
        WaitUntilForgotten(OsuClientKind.Stable);
        var library = Path.Combine(songs.Path, "Songs");
        var path = Add("Songs/set", 1, SongFolder.Osu());
        var stable = Process(OsuClientKind.Unknown, Path.Combine(songs.Path, "osu!.exe"), 1);
        var elsewhere = Process(
            OsuClientKind.Stable,
            Path.Combine(songs.Path, "other", "osu!.exe"),
            2
        );

        Assert.Equal(
            "no_process",
            Find(
                OsuClientKind.Stable,
                library,
                [elsewhere, Process(OsuClientKind.Stable), Process(OsuClientKind.Stable, "osu!")]
            ).Status
        );
        Assert.Equal(
            "no_editor_title",
            Find(OsuClientKind.Stable, library, [stable, elsewhere], (2, Map)).Status
        );

        var result = Find(
            OsuClientKind.Stable,
            library + Path.DirectorySeparatorChar,
            [stable],
            (1, "osu!"),
            (1, "osu!  - " + Map)
        );
        Assert.Equal("folder_found", result.Status);
        Assert.Equal(path, result.FolderPath);
    }

    [Fact]
    public void OpenMap_OfStable_NeedsAFolderAroundItsSongs()
    {
        Assert.Equal(
            "songs_folder_not_found",
            Find(
                OsuClientKind.Stable,
                Path.GetPathRoot(songs.Path),
                [Process(OsuClientKind.Stable)]
            ).Status
        );
    }

    [Fact]
    public void Songs_AreNextToTheClient()
    {
        var folder = Path.GetDirectoryName(songs.Write("osu!/osu!.exe"))!;
        var client = Path.Combine(folder, "osu!.exe");
        Assert.Null(BeatmapService.FindSongsNextTo(client));

        var expected = Directory.CreateDirectory(Path.Combine(folder, "Songs")).FullName;
        Assert.Equal(expected, BeatmapService.FindSongsNextTo(client));
        Assert.Equal(expected, BeatmapService.FindSongsNextTo($"\"{client}\""));
        Assert.Equal(expected, BeatmapService.FindSongsNextTo($"\"{client}\",1"));

        Assert.Null(BeatmapService.FindSongsNextTo("osu!.exe"));
        Assert.Null(BeatmapService.FindSongsNextTo(""));
        Assert.Null(BeatmapService.FindSongsNextTo(null));
    }

    [Fact]
    public void Songs_AreInTheProfileByDefault()
    {
        Assert.Null(BeatmapService.FindSongsInProfile(null));
        Assert.Null(BeatmapService.FindSongsInProfile(" "));
        Assert.Null(BeatmapService.FindSongsInProfile(songs.Path));

        var expected = Directory
            .CreateDirectory(Path.Combine(songs.Path, "AppData", "Local", "osu!", "Songs"))
            .FullName;
        Assert.Equal(expected, BeatmapService.FindSongsInProfile(songs.Path));
    }

    [Fact]
    public void OpenMap_WithoutAName_IsNotFound()
    {
        Assert.Equal(
            "metadata_detected",
            CurrentBeatmapLookupService.ResolveStableCurrentMap(songs.Path, ["osu! - "]).Status
        );
        Assert.Equal(
            "metadata_detected",
            CurrentBeatmapLookupService.ResolveStableCurrentMap(songs.Path, ["osu!", " "]).Status
        );
    }
}
