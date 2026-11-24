using MapsetVerifier.Server.Controller;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

/// <summary> Listing, searching and showing the mapsets of a Songs folder. </summary>
public sealed class BeatmapServiceLibraryTests : IDisposable
{
    private readonly SongFolder songs = new();

    public void Dispose() => songs.Dispose();

    /// <summary> Adds a mapset last edited the given minutes after the others' starting point. </summary>
    private void Add(string folder, int minute, string content, string file = "a.osu")
    {
        var path = songs.Write($"{folder}/{file}", content);
        var time = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddMinutes(minute);
        File.SetLastWriteTimeUtc(path, time);
    }

    private ApiBeatmapPage Get(
        string? search = null,
        int page = 0,
        int pageSize = 16,
        params string[] bookmarked
    ) => BeatmapService.GetBeatmaps(songs.Path, search, page, pageSize, bookmarked.ToHashSet());

    private static string[] Folders(ApiBeatmapPage page) =>
        page.Items.Select(item => item.Folder).ToArray();

    [Fact]
    public void MostRecentlyEditedDifficulty_ComesFirst()
    {
        Add("old", 1, SongFolder.Osu());
        Add("new", 3, SongFolder.Osu());
        Add("middle", 2, SongFolder.Osu());

        Assert.Equal(["new", "middle", "old"], Folders(Get()));
    }

    [Fact]
    public void Pages_ContinueWhereTheLastEnded()
    {
        for (var i = 0; i < 5; ++i)
            Add($"set {i}", -i, SongFolder.Osu());

        var first = Get(pageSize: 2);
        var last = Get(page: 2, pageSize: 2);

        Assert.Equal(["set 0", "set 1"], Folders(first));
        Assert.True(first.HasMore);
        Assert.Equal(["set 2", "set 3"], Folders(Get(page: 1, pageSize: 2)));
        Assert.Equal(["set 4"], Folders(last));
        Assert.False(last.HasMore);
        Assert.Empty(Folders(Get(page: 9, pageSize: 2)));
    }

    [Theory]
    [InlineData("freedom", "a")]
    [InlineData("xi", "a")]
    [InlineData("MAPPER B", "b")]
    [InlineData("222", "b")]
    [InlineData("FREEDOM DIVE - xi", "a")]
    [InlineData("nothing like it", null)]
    public void Search_LooksThroughMetadataAndIds(string search, string? expected)
    {
        Add("a", 1, SongFolder.Osu(title: "FREEDOM DiVE", artist: "xi", beatmapSetId: 111));
        Add("b", 2, SongFolder.Osu(title: "Other", creator: "Mapper B", beatmapSetId: 222));

        Assert.Equal(expected == null ? [] : [expected], Folders(Get(search)));
    }

    [Fact]
    public void Bookmarks_LimitTheList()
    {
        Add("a", 1, SongFolder.Osu());
        Add("b", 2, SongFolder.Osu());

        Assert.Equal(["a"], Folders(Get(bookmarked: ["a", "gone"])));
    }

    [Fact]
    public void FoldersWithoutDifficulties_AreLeftOut()
    {
        Add("set", 1, SongFolder.Osu());
        songs.Write("no difficulties/audio.mp3");

        Assert.Equal(["set"], Folders(Get()));
    }

    [Fact]
    public void Metadata_ComesFromTheFirstDifficultyThatHasAny()
    {
        Add("set", 1, "", "a.osu");
        Add(
            "set",
            1,
            SongFolder.Osu(
                title: "Song",
                artist: "Band",
                creator: "Mapper",
                beatmapId: 12,
                beatmapSetId: 34,
                events: ["0,0,\"bg.jpg\",0,0"]
            ),
            "b.osu"
        );

        var item = Assert.Single(Get().Items);

        Assert.Equal("Song", item.Title);
        Assert.Equal("Band", item.Artist);
        Assert.Equal("Mapper", item.Creator);
        Assert.Equal("12", item.BeatmapId);
        Assert.Equal("34", item.BeatmapSetId);
        Assert.Equal("/beatmap/image?folder=set", item.BackgroundPath);
    }

    [Fact]
    public void WithoutABackground_ThereIsNoImageUrl()
    {
        Add("set", 1, SongFolder.Osu());

        Assert.Equal("", Assert.Single(Get().Items).BackgroundPath);
    }

    [Fact]
    public void MissingSongsFolder_IsEmpty()
    {
        var page = BeatmapService.GetBeatmaps(
            System.IO.Path.Combine(songs.Path, "missing"),
            null,
            0,
            16
        );

        Assert.Empty(page.Items);
        Assert.False(page.HasMore);
    }

    [Fact]
    public void Info_OfOneMapset()
    {
        Add("set", 1, SongFolder.Osu(title: "Song", beatmapId: 12));
        var folder = System.IO.Path.Combine(songs.Path, "set");

        var info = BeatmapService.GetBeatmapInfo(folder)!.Value;

        Assert.Equal("Song", info.Title);
        Assert.Equal(12ul, info.BeatmapId);
        Assert.Null(info.BeatmapSetId); // -1 when it isn't submitted.
        Assert.Null(BeatmapService.GetBeatmapInfo(System.IO.Path.Combine(songs.Path, "missing")));
        Assert.Null(BeatmapService.GetBeatmapInfo(songs.Path));
    }

    [Fact]
    public void Controller_AnswersNotFound_OnlyForAnEmptyFirstPage()
    {
        Add("set", 1, SongFolder.Osu());
        var controller = new BeatmapController();

        Assert.IsType<OkObjectResult>(controller.GetBeatmaps(songs.Path, null).Result);
        Assert.IsType<NotFoundObjectResult>(controller.GetBeatmaps(songs.Path, "nope").Result);
        Assert.IsType<OkObjectResult>(controller.GetBeatmaps(songs.Path, null, 5).Result);
    }

    [Theory]
    [InlineData(-3, 0, 0, 16)]
    [InlineData(0, 5000, 0, 100)]
    public void Controller_KeepsPagesWithinBounds(int page, int size, int usedPage, int usedSize)
    {
        Add("set", 1, SongFolder.Osu());

        var ok = (OkObjectResult)
            new BeatmapController().GetBeatmaps(songs.Path, null, page, size).Result!;
        var result = (ApiBeatmapPage)ok.Value!;

        Assert.Equal(usedPage, result.Page);
        Assert.Equal(usedSize, result.PageSize);
    }

    private void AddImage(string name, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        if (name.EndsWith(".png"))
            image.SaveAsPng(stream);
        else
            image.SaveAsJpeg(stream);

        songs.Write(name, stream.ToArray());
    }

    [Theory]
    [InlineData("bg.png", "image/png")]
    [InlineData("bg.jpg", "image/jpeg")]
    public void Image_IsAThumbnail_KeepingItsShape(string name, string mime)
    {
        Add("set", 1, SongFolder.Osu(events: [$"0,0,\"{name}\",0,0"]));
        AddImage($"set/{name}", 1260, 504);

        var result = BeatmapService.GetBeatmapImage("set", songsFolderOverride: songs.Path);
        using var thumbnail = result.DataStream;

        Assert.True(result.Success);
        Assert.Equal(mime, result.MimeType);
        var info = Image.Identify(thumbnail!);
        Assert.Equal(315, info.Width);
        Assert.Equal(126, info.Height);
    }

    [Fact]
    public void Image_InFullSize_OrAlreadySmall_IsTheFileItself()
    {
        Add("set", 1, SongFolder.Osu(events: ["0,0,\"bg.png\",0,0"]));
        AddImage("set/bg.png", 1260, 504);
        Add("small", 1, SongFolder.Osu(events: ["0,0,\"bg.png\",0,0"]));
        AddImage("small/bg.png", 100, 100);

        var original = BeatmapService.GetBeatmapImage("set", true, songs.Path);
        var small = BeatmapService.GetBeatmapImage("small", songsFolderOverride: songs.Path);

        Assert.EndsWith("bg.png", original.ImagePath);
        Assert.Null(original.DataStream);
        Assert.EndsWith("bg.png", small.ImagePath);
    }

    [Fact]
    public void Image_TheClientAlreadyHas_IsNotMadeAgain()
    {
        Add("set", 1, SongFolder.Osu(events: ["0,0,\"bg.png\",0,0"]));
        AddImage("set/bg.png", 1260, 504);

        var first = BeatmapService.GetBeatmapImage("set", songsFolderOverride: songs.Path);
        first.DataStream?.Dispose();
        var again = BeatmapService.GetBeatmapImage(
            "set",
            songsFolderOverride: songs.Path,
            ifNoneMatch: first.ETag
        );
        var original = BeatmapService.GetBeatmapImage("set", true, songs.Path);

        Assert.Equal(first.ETag, again.ETag);
        Assert.Null(again.DataStream);
        Assert.NotEqual(first.ETag, original.ETag);
    }

    [Fact]
    public void Image_ThatIsMissing_IsAnError()
    {
        Add("set", 1, SongFolder.Osu(events: ["0,0,\"bg.png\",0,0"]));

        Assert.Equal(
            "No background image found.",
            BeatmapService.GetBeatmapImage("set", songsFolderOverride: songs.Path).ErrorMessage
        );
        Assert.Equal(
            "Beatmap folder not found.",
            BeatmapService.GetBeatmapImage("nope", songsFolderOverride: songs.Path).ErrorMessage
        );
    }

    [Fact]
    public void BlankDifficulties_AreListedWithoutMetadata()
    {
        Add("blank", 1, " ");
        Add("partial", 2, "osu file format v14\n[Metadata]\nTitle:Only a title");

        var items = Get().Items.ToList();

        Assert.Equal(["partial", "blank"], items.Select(item => item.Folder));
        Assert.Equal(["Only a title", ""], items.Select(item => item.Title));
        Assert.Equal(["", ""], items.Select(item => item.Artist));
        Assert.Equal(["", ""], items.Select(item => item.BeatmapId));
        Assert.Equal(["", ""], items.Select(item => item.BackgroundPath));
    }
}
