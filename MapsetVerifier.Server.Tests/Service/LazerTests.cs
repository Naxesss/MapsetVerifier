using MapsetVerifier.Server.Controller;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using MapsetVerifier.Server.Service.OsuRuntime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using osu.Game.Beatmaps;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

public sealed class LazerTests : IDisposable
{
    private readonly LazerLibrary lazer = new();

    public void Dispose() => lazer.Dispose();

    private ApiBeatmapPage Page(string? search = null, int page = 0, int size = 16) =>
        BeatmapService.GetLazerBeatmaps(search, page, size, lazer.DataPath);

    private static string[] Titles(ApiBeatmapPage page) =>
        page.Items.Select(item => item.Title).ToArray();

    [Fact]
    public void Library_IsSortedByWhatWasLastAddedOrEdited()
    {
        lazer.AddSet("Old", day: 1);
        lazer.AddSet("New", day: 5);
        lazer.AddSet("Edited", day: 2, editedDay: 9);
        lazer.AddSet("Edited before it was added", day: 4, editedDay: 1);
        lazer.AddSet("Deleted", day: 10, deleted: true);
        lazer.AddSet("Empty", day: 10, versions: []);

        Assert.Equal(["Edited", "New", "Edited before it was added", "Old"], Titles(Page()));
    }

    [Fact]
    public void Library_HasPages()
    {
        for (var i = 1; i <= 5; i++)
            lazer.AddSet($"{i}", day: i);

        Assert.Equal(["5", "4"], Titles(Page(size: 2)));
        Assert.True(Page(size: 2).HasMore);
        Assert.Equal(["1"], Titles(Page(page: 2, size: 2)));
        Assert.False(Page(page: 2, size: 2).HasMore);
    }

    [Theory]
    [InlineData("FREEDOM", true)]
    [InlineData("freedom dive - xi", true)]
    [InlineData("nakagawa", true)]
    [InlineData("39804", true)]
    [InlineData("398040", true)]
    [InlineData("other", false)]
    public void Library_IsSearchedByMetadataAndIds(string search, bool expected)
    {
        lazer.AddSet("FREEDOM DiVE", "xi", "Nakagawa-Kanon", onlineId: 39804);
        lazer.AddSet("Something", "Else", "Someone");

        Assert.Equal(expected ? ["FREEDOM DiVE"] : [], Titles(Page(search)));
        Assert.Equal(2, Titles(Page(" ")).Length);
    }

    [Fact]
    public void Library_HasWhatTheListShows()
    {
        var id = lazer.AddSet("A title", "An artist", "A mapper", onlineId: 12, day: 3);
        var unsubmitted = lazer.AddSet("Other", creator: null, onlineId: 0, day: 1);
        var items = Page().Items.ToList();

        Assert.Equal(id.ToString(), items[0].Folder);
        Assert.Equal("An artist", items[0].Artist);
        Assert.Equal("A mapper", items[0].Creator);
        Assert.Equal("12", items[0].BeatmapSetId);
        Assert.Equal("120", items[0].BeatmapId);
        Assert.Equal(
            $"/beatmap/lazer/image?id={id}&v={LazerLibrary.Date(3).UtcTicks:x}",
            items[0].BackgroundPath
        );

        Assert.Equal(unsubmitted.ToString(), items[1].Folder);
        Assert.Equal("", items[1].Creator);
        Assert.Equal("", items[1].BeatmapSetId);
        Assert.Equal("", items[1].BeatmapId);
    }

    [Fact]
    public void Library_IsReadAgain_OnlyOnceItChanged()
    {
        lazer.AddSet("First");
        Assert.Equal(["First"], Titles(Page()));
        Assert.Equal(["First"], Titles(Page()));

        lazer.AddSet("Second", day: 2);
        Assert.Equal(["Second", "First"], Titles(Page()));
    }

    [Fact]
    public void Library_ThatCannotBeRead_IsEmpty()
    {
        using var broken = new SongFolder();
        broken.Write("client.realm", "not a database");

        Assert.NotEqual(broken.Path, lazer.DataPath);
        Assert.Empty(LazerRealmService.GetBeatmapSets(broken.Path, null, 0, 16).Items);
        Assert.Null(LazerRealmService.GetBeatmapSetFiles(broken.Path, $"{Guid.NewGuid()}"));
        Assert.Null(LazerRealmService.ResolveLiveBeatmapSetId(broken.Path, $"{Guid.NewGuid()}"));
        Assert.Null(LazerRealmService.GetBackgroundFilename(broken.Path, $"{Guid.NewGuid()}"));
        Assert.Null(LazerRealmService.FindBestMatchingBeatmap(broken.Path, "", "Title", "", ""));
    }

    [Fact]
    public void DataFolder_IsTheGivenOne_OrTheUsualOne()
    {
        using var other = new SongFolder();

        // Only looked for on Windows, elsewhere it has to be given.
        var usual = OperatingSystem.IsWindows() ? lazer.DataPath : null;

        Assert.Equal(usual, LazerRealmService.DetectLazerDataDirectory());
        Assert.Equal(usual, LazerRealmService.ResolveLazerDataDirectory(null));
        // Without a database it isn't one.
        Assert.Equal(usual, LazerRealmService.ResolveLazerDataDirectory(other.Path));
        Assert.Equal(lazer.DataPath, LazerRealmService.ResolveLazerDataDirectory(lazer.DataPath));

        other.Write("client.realm");
        Assert.Equal(other.Path, LazerRealmService.ResolveLazerDataDirectory(other.Path));

        Environment.SetEnvironmentVariable("APPDATA", other.Path);
        Assert.Null(LazerRealmService.DetectLazerDataDirectory());
        Environment.SetEnvironmentVariable("APPDATA", null);
        Assert.Null(LazerRealmService.DetectLazerDataDirectory());
    }

    [Fact]
    public void WithoutADataFolder_ThereIsNothing()
    {
        Environment.SetEnvironmentVariable("APPDATA", null);
        var id = Guid.NewGuid().ToString();
        var controller = Controller();

        Assert.Empty(BeatmapService.GetLazerBeatmaps(null, 0, 16).Items);
        Assert.Contains("data folder", BeatmapService.GetLazerBeatmapImage(id).ErrorMessage);
        Assert.Contains("data folder", BeatmapService.MaterializeLazerBeatmap(id).ErrorMessage);
        Assert.IsType<NotFoundObjectResult>(controller.GetLazerDataDir());
        Assert.Equal(
            "lazer_data_dir_not_found",
            CurrentBeatmapLookupService.ResolveLazerCurrentMap("a", null).Status
        );
    }

    [Fact]
    public void Files_AreThoseOfTheSet()
    {
        var id = lazer.AddSet(files: [("a.osu", "aa1"), ("sub/b.png", "bb2"), ("", "cc3")]);
        var deleted = lazer.AddSet(deleted: true, files: [("a.osu", "aa1")]);

        Assert.Equal(
            [("a.osu", "aa1"), ("sub/b.png", "bb2")],
            LazerRealmService.GetBeatmapSetFiles(lazer.DataPath, id.ToString())!
        );
        Assert.Null(LazerRealmService.GetBeatmapSetFiles(lazer.DataPath, deleted.ToString()));
        Assert.Null(LazerRealmService.GetBeatmapSetFiles(lazer.DataPath, $"{Guid.NewGuid()}"));
        Assert.Null(LazerRealmService.GetBeatmapSetFiles(lazer.DataPath, "no id"));
    }

    [Fact]
    public void DeletedSet_IsReplacedByItsRedownload()
    {
        string? Resolve(object id) =>
            LazerRealmService.ResolveLiveBeatmapSetId(lazer.DataPath, id.ToString()!);

        var old = lazer.AddSet(onlineId: 5, deleted: true);
        var older = lazer.AddSet(onlineId: 5, deleted: true);
        var live = lazer.AddSet(onlineId: 5);
        var gone = lazer.AddSet(onlineId: 6, deleted: true);
        // Unsubmitted ones all share their id.
        var unsubmitted = lazer.AddSet(deleted: true);
        lazer.AddSet();

        Assert.Equal(live.ToString(), Resolve(old));
        Assert.Equal(live.ToString(), Resolve(older));
        Assert.Equal(live.ToString(), Resolve(live));
        Assert.Null(Resolve(gone));
        Assert.Null(Resolve(unsubmitted));
        Assert.Null(Resolve(Guid.NewGuid()));
        Assert.Null(Resolve("no id"));
    }

    private ApiLazerMaterializeResult Materialize(Guid id) =>
        BeatmapService.MaterializeLazerBeatmap(id.ToString(), lazer.DataPath);

    private static string[] FilesIn(string path) =>
        Directory
            .GetFiles(path, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(path, file).Replace('\\', '/'))
            .Order()
            .ToArray();

    [Fact]
    public void Materializing_WritesTheFilesUnderTheirNames()
    {
        lazer.AddFile("aa1", SongFolder.Osu());
        lazer.AddFile("bb2", "image");
        var id = lazer.AddSet(
            files: [("a.osu", "aa1"), ("sub/b.png", "bb2"), ("missing.mp3", "cc3")]
        );

        var result = Materialize(id);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(id.ToString(), result.BeatmapSetId);
        Assert.Equal(["a.osu", "sub/b.png"], FilesIn(result.FolderPath!));
        Assert.Equal("image", File.ReadAllText(Path.Combine(result.FolderPath!, "sub", "b.png")));
    }

    [Fact]
    public void Materializing_Again_OnlyKeepsWhatIsStillThere()
    {
        lazer.AddFile("aa1", "old");
        lazer.AddFile("aa2", "new");
        lazer.AddFile("bb1", "image");
        var id = lazer.AddSet(files: [("a.osu", "aa1"), ("b.png", "bb1")]);
        var path = Materialize(id).FolderPath!;
        File.WriteAllText(Path.Combine(path, "left over.osu.mvtmp"), "");

        lazer.Write(realm =>
        {
            var files = realm.Find<BeatmapSetInfo>(id)!.Files;
            files.Remove(files.Single(file => file.Filename == "b.png"));
            files.Single().File = realm.Add(new osu.Game.Models.RealmFile { Hash = "aa2" });
        });

        Assert.Equal(path, Materialize(id).FolderPath);
        Assert.Equal(["a.osu"], FilesIn(path));
        Assert.Equal("new", File.ReadAllText(Path.Combine(path, "a.osu")));
    }

    [Fact]
    public void Materializing_WhileAFileIsOpen_KeepsTheRest()
    {
        // Elsewhere, open files can still be replaced and removed.
        if (!OperatingSystem.IsWindows())
            return;

        lazer.AddFile("aa1", "a");
        lazer.AddFile("bb1", "b");
        var id = lazer.AddSet(files: [("a.osu", "aa1"), ("b.png", "bb1")]);
        var path = Materialize(id).FolderPath!;

        using (
            File.Open(Path.Combine(path, "a.osu"), FileMode.Open, FileAccess.Read, FileShare.None)
        )
        using (
            File.Open(Path.Combine(path, "b.png"), FileMode.Open, FileAccess.Read, FileShare.None)
        )
        {
            var failed = Materialize(id);

            Assert.False(failed.Success);
            Assert.Contains("Failed to prepare", failed.ErrorMessage);

            // Nor can it be removed, once it's no longer part of the set.
            lazer.AddFile("cc1", "c");
            lazer.Write(realm =>
            {
                var files = realm.Find<BeatmapSetInfo>(id)!.Files;
                files.Clear();
                files.Add(
                    new osu.Game.Models.RealmNamedFileUsage(
                        realm.Add(new osu.Game.Models.RealmFile { Hash = "cc1" }),
                        "c.osu"
                    )
                );
            });

            Assert.True(Materialize(id).Success);
            Assert.Equal(["a.osu", "b.png", "c.osu"], FilesIn(path));
        }

        Assert.True(Materialize(id).Success);
        Assert.Equal(["c.osu"], FilesIn(path));
    }

    [Fact]
    public void Materializing_WithoutAnyFiles_Fails()
    {
        var empty = Materialize(lazer.AddSet(files: [("a.osu", "aa1")]));

        Assert.False(empty.Success);
        Assert.Contains("No files", empty.ErrorMessage);
        Assert.Contains("could not be found", Materialize(Guid.NewGuid()).ErrorMessage);
        Assert.Contains(
            "could not be found",
            Materialize(lazer.AddSet(onlineId: 9, deleted: true)).ErrorMessage
        );
    }

    /// <summary> Names come from whoever made the mapset, so they must not lead elsewhere. </summary>
    [Fact]
    public void Materializing_StaysInItsFolder()
    {
        var name = $"{Guid.NewGuid():N}.txt";
        var absolute = Path.Combine(lazer.AppDataPath, name);
        lazer.AddFile("aa1", "a");
        lazer.AddFile("bb1", "outside");
        var id = lazer.AddSet(files: [("a.osu", "aa1"), ($"../{name}", "bb1"), (absolute, "bb1")]);

        var path = Materialize(id).FolderPath!;
        var parent = Path.Combine(Path.GetDirectoryName(path)!, name);

        try
        {
            Assert.Equal(["a.osu"], FilesIn(path));
            Assert.False(File.Exists(parent));
            Assert.False(File.Exists(absolute));
        }
        finally
        {
            File.Delete(parent);
        }
    }

    private BeatmapImageResult Background(Guid id, bool original = false) =>
        BeatmapService.GetLazerBeatmapImage(id.ToString(), original, lazer.DataPath);

    [Fact]
    public void Background_IsReadFromItsHash()
    {
        lazer.AddFile("bb1", SongFolder.Image(1920, 1080));
        var id = lazer.AddSet(background: "BG.png", files: [("bg.png", "bb1")]);

        var full = Background(id, true);
        Assert.True(full.Success, full.ErrorMessage);
        // The file itself has no extension to tell.
        Assert.Equal("image/png", full.MimeType);
        Assert.Equal(lazer.FilePath("bb1"), full.ImagePath);

        var thumbnail = Background(id);
        Assert.NotNull(thumbnail.DataStream);
        Assert.NotNull(thumbnail.ETag);
    }

    [Fact]
    public void Background_ThatIsMissing_IsAnError()
    {
        string? Error(string background, params (string, string)[] files) =>
            Background(lazer.AddSet(background: background, files: files)).ErrorMessage;

        Assert.Contains("No background", Error(""));
        Assert.Contains("No background", Error("video.mp4", ("video.mp4", "aa1")));
        Assert.Contains("could not be resolved", Error("bg.png", ("other.png", "aa1")));
        Assert.Contains("missing on disk", Error("bg.png", ("bg.png", "aa1")));
        Assert.Contains("No background", Background(Guid.NewGuid()).ErrorMessage);
        Assert.Null(LazerRealmService.GetBackgroundFilename(lazer.DataPath, "no id"));
    }

    private BeatmapController Controller(string? etag = null)
    {
        var context = new DefaultHttpContext();

        if (etag != null)
            context.Request.Headers.IfNoneMatch = etag;

        return new BeatmapController
        {
            ControllerContext = new ControllerContext { HttpContext = context },
        };
    }

    [Fact]
    public void Controller_ServesBackgrounds()
    {
        lazer.AddFile("bb1", SongFolder.Image(1920, 1080));
        var id = lazer.AddSet(background: "bg.png", files: [("bg.png", "bb1")]).ToString();

        using (
            var full = Assert
                .IsType<FileStreamResult>(
                    Controller().GetLazerBeatmapImage(id, true, lazer.DataPath)
                )
                .FileStream
        )
            Assert.Equal(new FileInfo(lazer.FilePath("bb1")).Length, full.Length);

        var controller = Controller();
        Assert.IsType<FileStreamResult>(controller.GetLazerBeatmapImage(id, false, lazer.DataPath));
        var etag = controller.Response.Headers.ETag.ToString();

        Assert.Equal(
            304,
            Assert
                .IsType<StatusCodeResult>(
                    Controller(etag).GetLazerBeatmapImage(id, false, lazer.DataPath)
                )
                .StatusCode
        );
        Assert.IsType<NotFoundObjectResult>(
            Controller().GetLazerBeatmapImage($"{Guid.NewGuid()}", false, lazer.DataPath)
        );
    }

    [Fact]
    public void Controller_ListsAndMaterializes()
    {
        lazer.AddFile("aa1", "a");
        var id = lazer.AddSet("A title", files: [("a.osu", "aa1")]);

        IActionResult List(string? search, int page, int size) =>
            Controller().GetLazerBeatmaps(search, page, size, lazer.DataPath).Result!;

        Assert.Single(
            Assert
                .IsType<ApiBeatmapPage>(Assert.IsType<OkObjectResult>(List(null, -1, 0)).Value)
                .Items
        );
        Assert.IsType<OkObjectResult>(List(null, 0, 1000));
        Assert.IsType<NotFoundObjectResult>(List("nothing like it", 0, 16));

        MaterializeLazerBeatmapRequest Request(string set) =>
            new() { BeatmapSetId = set, LazerDataDir = lazer.DataPath };

        Assert.IsType<OkObjectResult>(
            Controller().MaterializeLazerBeatmap(Request(id.ToString())).Result
        );
        Assert.IsType<NotFoundObjectResult>(
            Controller().MaterializeLazerBeatmap(Request("no id")).Result
        );

        if (OperatingSystem.IsWindows())
            Assert.IsType<OkObjectResult>(Controller().GetLazerDataDir());
        else
            Assert.IsType<NotFoundObjectResult>(Controller().GetLazerDataDir());
    }

    private string? Best(string? artist, string? title, string? creator, string? version) =>
        LazerRealmService
            .FindBestMatchingBeatmap(lazer.DataPath, artist, title, creator, version)
            ?.SetId;

    [Fact]
    public void OpenMap_IsTheSetMatchingMostOfIt()
    {
        var plain = lazer.AddSet("Song", "X", "Y").ToString();
        var artist = lazer.AddSet("Song", "Artist", "Y").ToString();
        var both = lazer.AddSet("Song!", "Artist", "Mapper").ToString();
        var version = lazer.AddSet("Song", "X", "Y", versions: ["Easy", "Hard"]).ToString();
        lazer.AddSet("Other", "Artist", "Mapper", versions: ["Hard"]);
        lazer.AddSet("Song", "Artist", "Mapper", versions: ["Hard"], deleted: true);

        Assert.Equal(both, Best("Artist", "Song", "Mapper", "Hard"));
        Assert.Equal(artist, Best("ARTIST", "song", "Y", "Insane"));
        Assert.Equal(version, Best("X", "Song", "Y", "hard"));
        Assert.Contains(Best(null, "Song", null, null), new[] { plain, artist, both, version });
        Assert.Null(Best("Artist", "Nothing", "Mapper", "Hard"));
        Assert.Null(Best("Artist", " ", "Mapper", "Hard"));
    }

    [Fact]
    public void OpenMap_HasTheIdsOfItsDifficulty()
    {
        lazer.AddSet("Song", "X", "Y", onlineId: 7, day: 4, versions: ["Easy", "Hard"]);

        var match = LazerRealmService
            .FindBestMatchingBeatmap(lazer.DataPath, "X", "Song", "Y", "Hard")!
            .Value;

        Assert.Equal(("71", "7"), (match.BeatmapId, match.BeatmapSetId));
        Assert.Equal(("Song", "X", "Y"), (match.Title, match.Artist, match.Creator));
        Assert.Equal($"{LazerLibrary.Date(4).UtcTicks:x}", match.VersionToken);
    }

    private ApiLazerLookupResult Open(string metadata, bool live = true) =>
        CurrentBeatmapLookupService.ResolveLazerCurrentMap(
            metadata,
            live ? metadata : null,
            lazer.DataPath
        );

    [Fact]
    public void OpenMap_IsMaterialized_AndAgainOnceItChanged()
    {
        const string title = "X - Song (Y) [Easy].osu";
        lazer.AddFile("aa1", "old");
        lazer.AddFile("aa2", "new");
        var id = lazer.AddSet("Song", "X", "Y", files: [("a.osu", "aa1")]);

        var result = Open(title);
        Assert.Equal("folder_found", result.Status);
        Assert.Equal(title, result.DetectedMetadata);
        Assert.Equal(lazer.DataPath, result.LookupRoot);
        Assert.Equal(id.ToString(), result.Beatmap!.Value.Folder);

        // Nothing changed, so it isn't copied again.
        var file = Path.Combine(result.FolderPath!, "a.osu");
        File.WriteAllText(file, "changed");
        Assert.Equal(result.FolderPath, Open(title).FolderPath);
        Assert.Equal("changed", File.ReadAllText(file));

        lazer.Write(realm =>
            realm.Find<BeatmapSetInfo>(id)!.Files.Single().File = realm.Add(
                new osu.Game.Models.RealmFile { Hash = "aa2" }
            )
        );
        Open(title);
        Assert.Equal("new", File.ReadAllText(file));

        Directory.Delete(result.FolderPath!, true);
        Open(title);
        Assert.Equal("new", File.ReadAllText(file));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OpenMap_ThatCannotBeFound_SaysWhy(bool live)
    {
        lazer.AddSet("Song", "X", "Y", files: [("a.osu", "aa1")]);

        foreach (
            var (metadata, reason) in new[]
            {
                ("unusual name.osu", "could not be parsed"),
                ("X - Nothing (Y) [Easy].osu", "matching mapset"),
            }
        )
        {
            var result = Open(metadata, live);

            Assert.Equal("metadata_detected", result.Status);
            Assert.Contains(reason, result.Message);
            Assert.Equal(!live, result.Message!.Contains("last detected"));
        }

        Assert.Contains("materialized", Open("X - Song (Y) [Easy].osu", live).Message);
    }

    /// <summary> Everything but the ids may be missing in the database. </summary>
    [Fact]
    public void SetsWithoutMetadata_AreListedToo()
    {
        var id = lazer.AddSet("Song", "X", "Y", versions: ["Easy", "Hard"]);
        lazer.Write(realm =>
        {
            foreach (var beatmap in realm.Find<BeatmapSetInfo>(id)!.Beatmaps)
            {
                beatmap.Metadata.Title = null!;
                beatmap.Metadata.Artist = null!;
                beatmap.Metadata.BackgroundFile = null!;
                beatmap.Metadata.Author = null!;
                beatmap.DifficultyName = null!;
            }
        });

        var item = Assert.Single(Page().Items);

        Assert.Equal(id.ToString(), item.Folder);
        Assert.Equal(("", "", ""), (item.Title, item.Artist, item.Creator));
        Assert.Null(Best("X", "Song", "Y", "Easy"));
        Assert.Null(LazerRealmService.GetBackgroundFilename(lazer.DataPath, id.ToString()));

        lazer.Write(realm =>
        {
            foreach (var beatmap in realm.Find<BeatmapSetInfo>(id)!.Beatmaps)
                beatmap.Metadata.Title = "Song";
        });
        Assert.Equal(id.ToString(), Best("X", "Song", "Y", "Easy"));
    }

    [Fact]
    public void Controller_LaterPagesMayBeEmpty()
    {
        lazer.AddSet("Only one");

        Assert.IsType<OkObjectResult>(
            Controller().GetLazerBeatmaps(null, 5, 16, lazer.DataPath).Result
        );
        Assert.IsType<NotFoundObjectResult>(
            Controller().GetLazerBeatmaps("nothing", 0, 16, lazer.DataPath).Result
        );

        lazer.Write(realm => realm.RemoveAll<BeatmapSetInfo>());
        Assert.IsType<NotFoundObjectResult>(
            Controller().GetLazerBeatmaps(null, 0, 16, lazer.DataPath).Result
        );
    }

    [Fact]
    public void Library_IsSortedByTheLastEditedDifficulty()
    {
        var id = lazer.AddSet("Edited", day: 1, versions: ["A", "B", "C"]);
        lazer.AddSet("Added", day: 5);
        lazer.Write(realm =>
        {
            var beatmaps = realm.Find<BeatmapSetInfo>(id)!.Beatmaps;
            beatmaps[0].LastLocalUpdate = LazerLibrary.Date(3);
            beatmaps[1].LastLocalUpdate = LazerLibrary.Date(7);
            beatmaps[2].LastLocalUpdate = LazerLibrary.Date(2);
        });

        Assert.Equal(["Edited", "Added"], Titles(Page()));
    }

    [Fact]
    public void Sets_WithoutMetadata_AreStillListed()
    {
        var id = lazer.AddSet(title: "Removed", editedDay: 5, versions: ["A", "B"]);
        lazer.Write(realm =>
        {
            var set = realm.Find<osu.Game.Beatmaps.BeatmapSetInfo>(id)!;
            set.Beatmaps[0].Metadata = null!;
            set.Beatmaps[1].LastLocalUpdate = LazerLibrary.Date(3);
        });

        var item = Assert.Single(
            LazerRealmService.GetBeatmapSets(lazer.DataPath, null, 0, 16).Items
        );

        Assert.Equal(("", "", ""), (item.Title, item.Artist, item.Creator));
        Assert.EndsWith($"&v={LazerLibrary.Date(5).UtcTicks:x}", item.BackgroundPath);
        Assert.Null(
            LazerRealmService.FindBestMatchingBeatmap(lazer.DataPath, "", "", "Nobody", "None")
        );
    }

    [Fact]
    public void Sets_DeletedSinceTheLibraryWasRead_AreNotOpened()
    {
        var id = lazer.AddSet(title: "B", artist: "A", versions: ["D"]);
        var path = Path.Combine(lazer.DataPath, "client.realm");

        Assert.NotNull(
            LazerRealmService.FindBestMatchingBeatmap(lazer.DataPath, "A", "B", "Creator", "D")
        );

        // Lazer doesn't always change when its library was written while it runs.
        var written = File.GetLastWriteTimeUtc(path);
        lazer.Write(realm =>
            realm.Find<osu.Game.Beatmaps.BeatmapSetInfo>(id)!.DeletePending = true
        );
        File.SetLastWriteTimeUtc(path, written);

        Assert.Null(
            LazerRealmService.FindBestMatchingBeatmap(lazer.DataPath, "A", "B", "Creator", "D")
        );
    }
}
