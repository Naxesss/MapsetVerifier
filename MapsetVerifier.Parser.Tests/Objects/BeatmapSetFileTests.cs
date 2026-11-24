using MapsetVerifier.Parser.Exceptions;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

/// <summary>Which files of a song folder a beatmapset is made of and uses.</summary>
public sealed class BeatmapSetFileTests : IDisposable
{
    private readonly string folder = Path.Combine(
        Path.GetTempPath(),
        "MapsetVerifierParserTests",
        Guid.NewGuid().ToString("N")
    );

    public BeatmapSetFileTests() => Directory.CreateDirectory(folder);

    public void Dispose()
    {
        try
        {
            Directory.Delete(folder, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private string Write(string name, string content = "")
    {
        var path = Path.Combine([folder, .. name.Split('/')]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);

        return path;
    }

    [Fact]
    public void MissingFolder_IsReported()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            new BeatmapSet(Path.Combine(folder, "missing"))
        );
    }

    [Fact]
    public void EmptyFolder_HasNoBeatmaps()
    {
        var set = new BeatmapSet(folder);

        Assert.Empty(set.Beatmaps);
        Assert.Null(set.GetOsbFileName());
        Assert.Null(set.GetAudioFilePath());
        Assert.Null(set.GetAudioFileName());
        Assert.Equal("No beatmaps in set.", set.ToString());
    }

    [Fact]
    public void EmptyOsuFile_IsListed_ButNotLoaded()
    {
        Write("good.osu", TestBeatmap.Code());
        Write("empty.osu", " \r\n ");

        var set = new BeatmapSet(folder);

        Assert.Single(set.Beatmaps);
        Assert.Equal(["empty.osu"], set.EmptyBeatmapFiles);
        Assert.Equal("Artist - Title (Creator)", set.ToString());
    }

    [Fact]
    public void BrokenOsuFile_SurfacesItsOwnException()
    {
        Write("broken.osu", TestBeatmap.Code(["not,a,hit,object,at all"]));

        var exception = Assert.Throws<InvalidBeatmapDataException>(() => new BeatmapSet(folder));

        Assert.Equal(
            "broken.osu: Could not read \"not,a,hit,object,at all\" in [HitObjects].",
            exception.Message
        );
    }

    [Fact]
    public void Storyboard_IsLoaded_OnlyIfNamedAfterTheSet()
    {
        const string osb = "[Events]\nSprite,Pass,Centre,\"a.png\",320,240";
        Write("map.osu", TestBeatmap.Code());
        var used = Write("Artist - Title (Creator).osb", osb);
        var unused = Write("Other - Title (Creator).osb", osb);

        var set = new BeatmapSet(folder);

        Assert.NotNull(set.Osb);
        Assert.True(set.IsFileUsed(used));
        Assert.False(set.IsFileUsed(unused));
    }

    [Fact]
    public void EmptyStoryboard_IsNotUsed()
    {
        Write("map.osu", TestBeatmap.Code());
        var osb = Write("Artist - Title (Creator).osb", "[Events]\n");

        Assert.False(new BeatmapSet(folder).IsFileUsed(osb));
    }

    [Fact]
    public void FilesOfEvents_AreUsed_WhateverTheirCase()
    {
        Write(
            "map.osu",
            TestBeatmap.Code(
                events:
                [
                    "0,0,\"BG.jpg\",0,0",
                    "Video,0,\"video.mp4\"",
                    "Sprite,Pass,Centre,\"SB\\star.png\",320,240",
                    "Sample,0,0,\"SB\\boom\",60",
                    "Animation,Pass,Centre,\"SB\\run.png\",320,240,2,100,LoopForever",
                ],
                general: "AudioFilename: Audio.mp3"
            )
        );
        string[] used =
        [
            Write("bg.JPG"),
            Write("video.mp4"),
            Write("audio.mp3"),
            Write("sb/Star.png"),
            Write("sb/boom.ogg"), // Referenced without its extension.
            Write("sb/run0.png"),
            Write("sb/run1.png"),
        ];
        string[] unused =
        [
            Write("star.png"), // Not in the folder the sprite says.
            Write("sb/run2.png"), // Past the last frame.
            Write("notes.txt"),
        ];

        var set = new BeatmapSet(folder);

        Assert.All(used, path => Assert.True(set.IsFileUsed(path), path));
        Assert.All(unused, path => Assert.False(set.IsFileUsed(path), path));
    }

    [Fact]
    public void HitSoundFiles_AreUsed_OnlyWithAMatchingIndexAndSampleset()
    {
        Write(
            "map.osu",
            TestBeatmap.Code(
                ["256,192,1000,1,2,0:0:0:0:", "256,192,2000,1,0,0:0:0:0:custom.wav"],
                timingPoints: ["0,500,4,2,2,100,1,0"]
            )
        );
        string[] used =
        [
            Write("soft-hitnormal2.wav"),
            Write("soft-hitwhistle2.ogg"),
            Write("custom.wav"),
        ];
        string[] unused =
        [
            Write("soft-hitnormal3.wav"),
            Write("soft-hitclap2.wav"),
            Write("drum-hitnormal2.wav"),
        ];

        var set = new BeatmapSet(folder);

        Assert.All(used, path => Assert.True(set.IsFileUsed(path), path));
        Assert.All(unused, path => Assert.False(set.IsFileUsed(path), path));
    }

    [Fact]
    public void TaikoHitSoundFile_DoesNotHideTheRegularOne()
    {
        Write(
            "map.osu",
            TestBeatmap.Code(["256,192,1000,1,0,0:0:0:0:"], timingPoints: ["0,500,4,2,2,100,1,0"])
        );
        var regular = Write("soft-hitnormal2.wav");
        var taiko = Write("taiko-soft-hitnormal2.wav");

        // Standard plays the regular one, its conversion to taiko the other.
        var set = new BeatmapSet(folder);

        Assert.True(set.IsFileUsed(taiko));
        Assert.True(set.IsFileUsed(regular));
    }

    [Fact]
    public void AudioFile_OfTheFirstBeatmap()
    {
        Write("map.osu", TestBeatmap.Code(general: "AudioFilename: song.mp3"));
        var withoutFile = new BeatmapSet(folder);
        Assert.Equal("song.mp3", withoutFile.GetAudioFileName());
        Assert.Null(withoutFile.GetAudioFilePath());

        var audio = Write("song.mp3");
        Assert.Equal(audio, new BeatmapSet(folder).GetAudioFilePath());
    }

    [Fact]
    public void Reloading_SeesChangedFiles()
    {
        Write("map.osu", TestBeatmap.Code([TestBeatmap.Circle(1000)]));
        Assert.Equal(1000, new BeatmapSet(folder).Beatmaps[0].GetHitObject(0)!.time);

        Write("map.osu", TestBeatmap.Code([TestBeatmap.Circle(4000)]));
        Assert.Equal(4000, new BeatmapSet(folder).Beatmaps[0].GetHitObject(0)!.time);
    }

    [Fact]
    public void FilesOfTheSetsStoryboard_AreUsed()
    {
        Write("map.osu", TestBeatmap.Code());
        Write(
            "Artist - Title (Creator).osb",
            string.Join(
                "\n",
                "[Events]",
                "0,0,\"bg.jpg\",0,0",
                "Video,0,\"video.mp4\"",
                "Sprite,Pass,Centre,\"star.png\",320,240",
                "Sample,0,0,\"boom.wav\",60",
                "Animation,Pass,Centre,\"run.png\",320,240,2,100,LoopForever"
            )
        );
        string[] used =
        [
            Write("bg.jpg"),
            Write("video.mp4"),
            Write("star.png"),
            Write("boom.wav"),
            Write("run0.png"),
        ];
        string[] unused = [Write("run2.png"), Write("no extension"), Write(".hidden")];

        var set = new BeatmapSet(folder);

        Assert.All(used, path => Assert.True(set.IsFileUsed(path), path));
        Assert.All(unused, path => Assert.False(set.IsFileUsed(path), path));
    }

    [Theory]
    [InlineData("[Events]\n0,0,\"missing.jpg\",0,0", null)]
    [InlineData("[Events]\n0,0,\"\",0,0", null)]
    [InlineData("[Events]\n0,0,\"BG.jpg\",0,0", "bg.jpg")]
    [InlineData("[Events]\nBackground,0,\"bg\",0,0", "bg.jpg")]
    [InlineData("[Events]\n2,0,100\n[TimingPoints]\n0,0,\"bg.jpg\",0,0", null)]
    [InlineData("[General]", null)]
    public void Background_IsReadFromTheEventsOnly(string code, string? expected)
    {
        Write("bg.jpg");
        Write("map.osu", code);

        Assert.Equal(
            expected,
            Path.GetFileName(Beatmap.ReadBackgroundFilePath(folder, "map.osu"))?.ToLower()
        );
    }
}
