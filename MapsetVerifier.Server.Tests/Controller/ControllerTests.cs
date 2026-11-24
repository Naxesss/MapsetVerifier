using System.Text;
using MapsetVerifier.Framework;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Controller;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Model.AudioAnalysis;
using MapsetVerifier.Server.Model.BeatmapAnalysis;
using MapsetVerifier.Server.Model.MetadataAnalysis;
using MapsetVerifier.Server.Model.VideoAnalysis;
using MapsetVerifier.Server.Service;
using MapsetVerifier.Snapshots.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Xunit;

namespace MapsetVerifier.Server.Tests.Controller;

/// <summary> What the app gets back: the result, or a status that says what went wrong. </summary>
public sealed class ControllerTests : IDisposable
{
    private readonly SongFolder song = new();
    private readonly SongFolder store = new();

    public ControllerTests()
    {
        SnapshotStore.ConfigurePath(store.Path, "externals");
        CheckRunHistoryService.ConfigurePath(store.Path, "externals");
        Checker.ConfigureCustomChecksPath(store.Path, "externals");
        CheckerRegistry.ClearChecks();
    }

    public void Dispose()
    {
        CheckerRegistry.ClearChecks();
        song.Dispose();
        store.Dispose();
    }

    private static T With<T>(T controller)
        where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } },
        };

        return controller;
    }

    private static int Status(IActionResult? result) =>
        result switch
        {
            IStatusCodeActionResult { StatusCode: { } code } => code,
            FileResult => 200,
            _ => throw new InvalidOperationException($"{result} has no status"),
        };

    private static int Status<T>(ActionResult<T> result) =>
        result.Result == null ? 200 : Status(result.Result);

    private string Missing => Path.Combine(store.Path, "missing");

    /// <summary> Exists, but can't be read as a mapset. </summary>
    private string Broken()
    {
        store.Write("broken/a.osu", SongFolder.Osu(hitObjects: ["not,a,hit,object"]));

        return Path.Combine(store.Path, "broken");
    }

    [Fact]
    public void BeatmapAnalysis()
    {
        song.Write("a.osu", SongFolder.Osu());
        var controller = With(new BeatmapAnalysisController());

        int[] Statuses(string folder)
        {
            var request = new BeatmapAnalysisRequest { BeatmapSetFolder = folder };

            return
            [
                Status(controller.AnalyzeBeatmap(request)),
                Status(controller.AnalyzeObjects(request)),
                Status(controller.AnalyzeDifficulty(request)),
            ];
        }

        Assert.Equal([200, 200, 200], Statuses(song.Path));
        Assert.Equal([400, 400, 400], Statuses(" "));
        Assert.Equal([404, 404, 404], Statuses(Missing));
        Assert.All(Statuses(Broken()), status => Assert.InRange(status, 404, 500));
    }

    [Fact]
    public void Metadata()
    {
        song.Write("a.osu", SongFolder.Osu(events: ["0,0,\"bg.jpg\",0,0"]));
        var controller = With(new MetadataAnalysisController());

        int[] Statuses(string folder)
        {
            var request = new MetadataAnalysisRequest { BeatmapSetFolder = folder };

            return
            [
                Status(controller.AnalyzeMetadata(request)),
                Status(controller.AnalyzeStoryboard(request)),
            ];
        }

        Assert.Equal([200, 200], Statuses(song.Path));
        Assert.Equal([400, 400], Statuses(""));
        Assert.Equal([404, 404], Statuses(Missing));
        Assert.All(Statuses(Broken()), status => Assert.InRange(status, 404, 500));
    }

    [Theory]
    [InlineData("bg.jpg", "image/jpeg")]
    [InlineData("bg.JPEG", "image/jpeg")]
    [InlineData("bg.png", "image/png")]
    [InlineData("bg.gif", "image/gif")]
    [InlineData("bg.bmp", "image/bmp")]
    [InlineData("bg.xyz", "application/octet-stream")]
    public void ResourceFile_IsServedByItsType(string name, string expected)
    {
        song.Write("a.osu", SongFolder.Osu(events: [$"0,0,\"{name}\",0,0"]));
        song.Write(name, "content");
        song.Write("secret.txt", "content");

        var controller = With(new MetadataAnalysisController());

        Assert.Equal(
            expected,
            Assert
                .IsType<PhysicalFileResult>(controller.GetResourceFile(song.Path, name))
                .ContentType
        );
        Assert.Equal(404, Status(controller.GetResourceFile(song.Path, "secret.txt")));
        Assert.Equal(404, Status(controller.GetResourceFile(song.Path, "../secret.txt")));
        Assert.Equal(400, Status(controller.GetResourceFile(song.Path, "")));
        Assert.Equal(400, Status(controller.GetResourceFile("", name)));
    }

    [Theory]
    [InlineData("soft-hitnormal2.wav", "audio/wav")]
    [InlineData("soft-hitnormal2.ogg", "audio/ogg")]
    [InlineData("soft-hitnormal2.mp3", "audio/mpeg")]
    public void HitSoundFile_IsServedByItsType(string name, string expected)
    {
        song.Write("a.osu", SongFolder.Osu(timingPoints: ["0,500,4,2,2,100,1,0"]));
        song.Write(name, SongFolder.Wave(0.1, SongFolder.Sine(0.5)));

        Assert.Equal(
            expected,
            Assert
                .IsType<PhysicalFileResult>(
                    With(new MetadataAnalysisController()).GetResourceFile(song.Path, name)
                )
                .ContentType
        );
    }

    [Theory]
    [InlineData("video.mp4", "video/mp4")]
    [InlineData("video.m4v", "video/mp4")]
    [InlineData("video.webm", "video/webm")]
    [InlineData("video.mov", "video/quicktime")]
    [InlineData("video.avi", "video/x-msvideo")]
    [InlineData("video.flv", "video/x-flv")]
    [InlineData("video.wmv", "video/x-ms-wmv")]
    [InlineData("video.mpg", "video/mpeg")]
    [InlineData("video.mpeg", "video/mpeg")]
    [InlineData("video.mkv", "video/x-matroska")]
    [InlineData("video.xyz", "application/octet-stream")]
    // What it really is comes first.
    [InlineData("video.avi", "video/mp4", "....ftypisom")]
    [InlineData("video.mp4", "video/x-msvideo", "RIFF....AVI ")]
    [InlineData("video.mp4", "video/x-flv", "FLV.........")]
    public void Video_IsServedByItsType(string name, string expected, string content = "unknown")
    {
        song.Write("a.osu", SongFolder.Osu(events: [$"Video,0,\"{name}\""]));
        song.Write(name, content);
        song.Write("other.mp4", "unused");
        var controller = With(new VideoAnalysisController());

        var file = Assert.IsType<FileStreamResult>(controller.StreamVideo(song.Path, name));
        file.FileStream.Dispose();

        Assert.Equal(expected, file.ContentType);
        Assert.True(file.EnableRangeProcessing);
        Assert.Equal(404, Status(controller.StreamVideo(song.Path, "other.mp4")));
        Assert.Equal(400, Status(controller.StreamVideo(song.Path, " ")));
        Assert.Equal(400, Status(controller.StreamVideo(" ", name)));
        Assert.InRange(Status(controller.StreamVideo(Missing, name)), 404, 500);
    }

    [Fact]
    public void VideoAnalysis()
    {
        song.Write("a.osu", SongFolder.Osu(events: ["Video,0,\"video.mp4\""]));
        var controller = With(new VideoAnalysisController());

        int StatusOf(string folder) =>
            Status(
                controller.AnalyzeVideos(new VideoAnalysisRequest { BeatmapSetFolder = folder })
            );

        Assert.Equal(200, StatusOf(song.Path));
        Assert.Equal(400, StatusOf(""));
        Assert.Equal(404, StatusOf(Missing));
        Assert.InRange(StatusOf(Broken()), 404, 500);
    }

    [Theory]
    [InlineData("audio.mp3", "audio/mpeg")]
    [InlineData("audio.ogg", "audio/ogg")]
    [InlineData("audio.wav", "audio/wav")]
    [InlineData("audio.flac", "audio/flac")]
    [InlineData("audio.m4a", "audio/mp4")]
    [InlineData("audio.xyz", "application/octet-stream")]
    public void Audio_IsServedByItsType(string name, string expected)
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: " + name]));
        var controller = With(new AudioAnalysisController());

        Assert.Equal(404, Status(controller.StreamAudio(song.Path)));
        song.Write(name, "content");
        Assert.Equal(
            expected,
            Assert.IsType<PhysicalFileResult>(controller.StreamAudio(song.Path)).ContentType
        );
        Assert.Equal(400, Status(controller.StreamAudio("")));
        Assert.InRange(Status(controller.StreamAudio(Missing)), 404, 500);
    }

    [Fact]
    public void AudioAnalysis()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write("audio.wav", SongFolder.Wave(1, SongFolder.Sine(0.5), SongFolder.Sine(0.4)));
        var controller = With(new AudioAnalysisController());

        int[] Statuses(string folder) =>
            [
                Status(
                    controller.AnalyzeAudio(new AudioAnalysisRequest { BeatmapSetFolder = folder })
                ),
                Status(
                    controller.GetSpectrogram(
                        new SpectrogramRequest { BeatmapSetFolder = folder, FftSize = 1024 }
                    )
                ),
                Status(
                    controller.GetFrequencyAnalysis(
                        new FrequencyAnalysisRequest { BeatmapSetFolder = folder, FftSize = 1024 }
                    )
                ),
            ];

        Assert.Equal([200, 200, 200], Statuses(song.Path));
        Assert.Equal([400, 400, 400], Statuses(""));
        Assert.All(Statuses(Missing), status => Assert.InRange(status, 404, 500));
    }

    [Fact]
    public void Library_HasSafePages()
    {
        song.Write("one/a.osu", SongFolder.Osu(title: "First"));
        song.Write("two/a.osu", SongFolder.Osu(title: "Second"));
        var controller = With(new BeatmapController());

        ApiBeatmapPage Page(
            string? search = null,
            int page = 0,
            int size = 16,
            string? bookmarks = null
        ) =>
            Assert.IsType<ApiBeatmapPage>(
                Assert
                    .IsType<OkObjectResult>(
                        controller.GetBeatmaps(song.Path, search, page, size, bookmarks).Result
                    )
                    .Value
            );

        Assert.Equal(2, Page().Items.Count());
        Assert.Equal(2, Page(page: -5, size: -1).Items.Count());
        Assert.Equal(100, Page(size: 5000).PageSize);
        Assert.Equal("First", Assert.Single(Page("first").Items).Title);
        Assert.Equal("two", Assert.Single(Page(bookmarks: "two, three")!.Items).Folder);
        Assert.Empty(Page(page: 4).Items);

        Assert.Equal(404, Status(controller.GetBeatmaps(song.Path, "nothing")));
        Assert.Equal(404, Status(controller.GetBeatmaps(Missing, null)));
    }

    [Fact]
    public void BeatmapInfo()
    {
        song.Write("a.osu", SongFolder.Osu(beatmapId: 5, beatmapSetId: 6));
        var controller = With(new BeatmapController());

        var result = controller.GetBeatmapInfo(new RunChecksRequest { Folder = song.Path });

        Assert.Equal(200, Status(result));
        Assert.Equal(
            404,
            Status(controller.GetBeatmapInfo(new RunChecksRequest { Folder = Missing }))
        );
        Assert.Equal(
            404,
            Status(controller.GetBeatmapInfo(new RunChecksRequest { Folder = store.Path }))
        );
    }

    [Fact]
    public void Checks()
    {
        song.Write("a.osu", SongFolder.Osu());
        var controller = With(new BeatmapController());

        Assert.Equal(
            200,
            Status(
                controller.RunBeatmapSetChecks(
                    new RunChecksRequest
                    {
                        Folder = song.Path,
                        IncludeCheckRunDelta = true,
                        CreateSnapshot = true,
                        IncludeCheckTimings = true,
                    }
                )
            )
        );
        Assert.Equal(
            500,
            Status(controller.RunBeatmapSetChecks(new RunChecksRequest { Folder = Missing }))
        );

        int Override(string folder, string difficulty) =>
            Status(
                controller.RunCheckWithOverride(
                    new RunCheckOverrideRequest
                    {
                        Folder = folder,
                        DifficultyName = difficulty,
                        OverrideDifficulty = Beatmap.Difficulty.Easy,
                    }
                )
            );

        Assert.Equal(200, Override(song.Path, "Hard"));
        Assert.Equal(404, Override(song.Path, "Unknown"));
        Assert.Equal(500, Override(Missing, "Hard"));

        Assert.Equal(204, Status(controller.ClearCheckRunHistory(song.Path)));
        Assert.Equal(400, Status(controller.ClearCheckRunHistory(" ")));
        Assert.Equal(500, Status(controller.ClearCheckRunHistory(Missing)));
    }

    [Theory]
    [InlineData(true, "structure", "complete")]
    [InlineData(false, "error", "error")]
    public async Task StreamedChecks_EndWithTheResultOrAnError(
        bool exists,
        string first,
        string last
    )
    {
        song.Write("a.osu", SongFolder.Osu());
        var controller = With(new BeatmapController());

        await controller.RunBeatmapSetChecksStream(
            new RunChecksRequest { Folder = exists ? song.Path : Missing },
            CancellationToken.None
        );

        var body = (MemoryStream)controller.Response.Body;
        var events = Encoding
            .UTF8.GetString(body.ToArray())
            .Split('\n')
            .Where(line => line.StartsWith("event:"))
            .Select(line => line["event:".Length..].Trim())
            .ToList();

        Assert.Equal("text/event-stream", controller.Response.ContentType);
        Assert.Equal(first, events[0]);
        Assert.Contains(last, events);
    }

    [Fact]
    public void Image_IsCachedByItsTag()
    {
        song.Write("one/a.osu", SongFolder.Osu(events: ["0,0,\"bg.png\",0,0"]));
        song.Write(
            "one/bg.png",
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
            )
        );

        foreach (var original in new[] { true, false })
        {
            var controller = With(new BeatmapController());
            var result = controller.GetBeatmapImage("one", original, song.Path);

            var file = Assert.IsType<FileStreamResult>(result);
            file.FileStream.Dispose();
            Assert.StartsWith("image/", file.ContentType);

            var tag = controller.Response.Headers.ETag.ToString();
            Assert.NotEmpty(tag);

            var again = With(new BeatmapController());
            again.Request.Headers.IfNoneMatch = tag;
            Assert.Equal(304, Status(again.GetBeatmapImage("one", original, song.Path)));
        }

        Assert.Equal(
            404,
            Status(With(new BeatmapController()).GetBeatmapImage("two", false, song.Path))
        );
    }

    [Fact]
    public void Snapshots()
    {
        song.Write("a.osu", SongFolder.Osu(beatmapId: 5, beatmapSetId: 6));
        var controller = With(new SnapshotController());

        Assert.Equal(
            200,
            Status(
                controller.Capture(new SnapshotCaptureRequest { Folder = song.Path, Pin = "First" })
            )
        );
        song.Write(
            "a.osu",
            SongFolder.Osu(
                beatmapId: 5,
                beatmapSetId: 6,
                hitObjects: ["256,192,1000,5,0,0:0:0:0:", "100,192,2000,1,0,0:0:0:0:"]
            )
        );
        Assert.Equal(
            200,
            Status(controller.GetHistory(new RunChecksRequest { Folder = song.Path }))
        );

        var entries = SnapshotStore.Load("set-6").Entries;
        Assert.Equal(2, entries.Count);

        Assert.Equal(
            200,
            Status(
                controller.Compare(
                    new SnapshotCompareRequest
                    {
                        SetKey = "set-6",
                        BaseId = entries[0].Id,
                        TargetId = entries[1].Id,
                    }
                )
            )
        );
        Assert.Equal(
            200,
            Status(
                controller.GetWindow(
                    new SnapshotWindowRequest
                    {
                        SetKey = "set-6",
                        BaseId = entries[0].Id,
                        TargetId = entries[1].Id,
                        DifficultyKey = "b-5",
                        Anchor = 2000,
                        AnchorEnd = 2000,
                    }
                )
            )
        );
        Assert.Equal(
            200,
            Status(
                controller.SetPin(
                    new SnapshotPinRequest
                    {
                        SetKey = "set-6",
                        Id = entries[1].Id,
                        Pin = "Second",
                    }
                )
            )
        );
        Assert.Equal("Second", SnapshotStore.Load("set-6").Entries[1].Pin);

        Assert.Equal(
            500,
            Status(controller.Compare(new SnapshotCompareRequest { SetKey = "set-6" }))
        );
    }

    [Fact]
    public void Documentation_AndPlugins()
    {
        var plugins = With(new PluginController());

        Assert.False(
            plugins
                .ReloadPlugins(new ApiPluginReloadRequest { CustomChecksEnabled = false })
                .CustomChecksEnabled
        );
        store.Write("externals/CustomChecks/broken.dll", "not an assembly");
        var report = plugins.ReloadPlugins(null);

        Assert.True(report.CustomChecksEnabled);
        Assert.Equal("broken.dll", Assert.Single(report.FailedPlugins).FileName);
        Assert.Equal("broken.dll", Assert.Single(plugins.GetPlugins().FailedPlugins).FileName);

        var documentation = With(new DocumentationController());
        Assert.Equal(404, Status(documentation.GetDetails(int.MaxValue)));
        Assert.NotNull(documentation.GetGeneral().ToList());
        Assert.NotNull(documentation.GetBeatmapDocumentation(Beatmap.Mode.Taiko).ToList());
    }

    [Fact]
    public void RankingCriteria()
    {
        var controller = With(new RankingCriteriaController());

        Assert.NotEmpty(controller.GetOverview().Pages);
        Assert.Equal(404, Status(controller.GetPage("no-such-page")));
        Assert.Equal(200, Status(controller.GetPage(controller.GetOverview().Pages[0].Key)));
        Assert.Empty(controller.GetStatements(["no/such/statement"]));
    }
}
