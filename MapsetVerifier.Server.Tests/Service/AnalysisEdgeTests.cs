using MapsetVerifier.Server.Service;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

/// <summary> The pages of the app cope with maps and files which are unfinished or out of the ordinary. </summary>
public sealed class AnalysisEdgeTests : IDisposable
{
    private readonly SongFolder song = new();

    public void Dispose() => song.Dispose();

    private static readonly Dictionary<
        string,
        (string[] Objects, string[] Timing, string[] Events)
    > Maps = new()
    {
        ["no objects"] = ([], ["0,500,4,2,0,100,1,0"], []),
        ["no objects or timing"] = ([], [], []),
        ["only green lines"] = (
            ["256,192,1000,1,0,0:0:0:0:"],
            ["0,-100,4,2,0,100,0,0", "0,-50,4,2,0,60,0,0"],
            []
        ),
        ["one object at the start"] = (["256,192,0,1,0,0:0:0:0:"], ["0,500,4,2,0,100,1,0"], []),
        ["everything before the first line"] = (
            ["256,192,1000,1,0,0:0:0:0:", "256,192,2000,12,0,3000,0:0:0:0:"],
            ["20000,500,4,2,0,100,1,1"],
            ["2,1200,1800"]
        ),
        ["same offsets"] = (
            ["256,192,1000,1,0,0:0:0:0:", "256,192,30000,1,2,0:0:0:0:"],
            [
                "0,500,4,2,0,100,1,0",
                "0,500,4,2,0,100,1,0",
                "0,-50,4,2,0,40,0,1",
                "5000,-100,4,2,0,40,0,0",
                "30000,-50,4,2,0,90,0,1",
            ],
            []
        ),
        ["sliders"] = (
            [
                "0,100,1000,6,2,L|140:100,3,140,0|2|8|4,0:0|1:2|2:0|3:1,0:0:0:0:",
                "0,100,8000,2,0,L|140:100,2,140",
            ],
            ["0,500,4,2,0,100,1,0"],
            []
        ),
    };

    public static TheoryData<string, int> EveryMapAndMode()
    {
        var data = new TheoryData<string, int>();

        foreach (var map in Maps.Keys)
            for (var mode = 0; mode < 4; mode++)
                data.Add(map, mode);

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryMapAndMode))]
    public void EveryPage_ShowsUnusualMaps(string map, int mode)
    {
        var (objects, timing, events) = Maps[map];
        song.Write(
            "a.osu",
            SongFolder.Osu(mode: mode, hitObjects: objects, timingPoints: timing, events: events)
        );

        var analysis = BeatmapAnalysisService.Analyze(song.Path);
        Assert.True(analysis.Success, analysis.ErrorMessage);

        var overview = BeatmapAnalysisService.AnalyzeObjects(song.Path);
        Assert.True(overview.Success, overview.ErrorMessage);

        var difficulty = BeatmapAnalysisService.AnalyzeDifficulty(song.Path);
        Assert.True(difficulty.Success, difficulty.ErrorMessage);

        var metadata = MetadataAnalysisService.Analyze(song.Path);
        Assert.True(metadata.Success, metadata.ErrorMessage);

        Assert.True(StoryboardDetailService.Analyze(song.Path).Success);
        Assert.True(VideoAnalysisService.AnalyzeVideos(song.Path).Success);
    }

    private const string Video = "Video,0,\"{0}\"";

    [Fact]
    public void Videos_AreReadFromDifficultiesAndTheStoryboard()
    {
        string[] Events(params string[] names) =>
            names.Select(name => string.Format(Video, name)).ToArray();

        song.Write("a.osu", SongFolder.Osu(version: "A", events: Events("one.mp4")));
        song.Write("b.osu", SongFolder.Osu(version: "B", events: Events("one.mp4", "gone.mp4")));
        song.Write(
            "Artist - Title (Creator).osb",
            "[Events]\n" + string.Join("\n", Events("one.mp4", "two.avi"))
        );
        song.Write("one.mp4", "not really a video");
        song.Write("two.avi", "not really a video");

        var videos = VideoAnalysisService.AnalyzeVideos(song.Path).Videos;

        Assert.Equal(3, videos.Count());
        Assert.Single(videos, video => !video.Exists);

        var metadata = MetadataAnalysisService.Analyze(song.Path);
        Assert.True(metadata.Success, metadata.ErrorMessage);

        Assert.Null(VideoAnalysisService.ResolveReferencedVideoPath(song.Path, "gone.mp4"));
        Assert.NotNull(VideoAnalysisService.ResolveReferencedVideoPath(song.Path, "one.mp4"));
    }

    [Fact]
    public void Colours_IncludeThoseOfSliders()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                timingPoints: ["0,500,4,2,0,100,1,1"],
                colours:
                [
                    "Combo1 : 255,255,255",
                    "Combo2 : 5,5,5",
                    "SliderBorder : 255,0,0",
                    "SliderTrackOverride : 0,255,0",
                ]
            )
        );

        var colours = Assert.Single(MetadataAnalysisService.Analyze(song.Path).ColourSettings);

        Assert.NotNull(colours.SliderBorder);
        Assert.NotNull(colours.SliderTrack);
    }

    /// <summary> Of which the given share of the time is as loud as it gets. </summary>
    private static byte[] Music(double quiet, double share) =>
        SongFolder.Wave(
            2,
            time => SongFolder.Sine(time % 0.1 < share * 0.1 ? 0.9 : quiet)(time),
            time => SongFolder.Sine(time % 0.1 < share * 0.1 ? 0.9 : quiet)(time)
        );

    private Model.AudioAnalysis.AudioAnalysisResult Analyze(string name, byte[] content)
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: other.wav"]));
        song.Write(name, content);

        return AudioAnalysisService.AnalyzeAudio(song.Path, name);
    }

    [Fact]
    public void Audio_WithMoreDynamics_IsLessCompressed()
    {
        var severities = new[] { (0.9, 1d), (0.2, 0.3), (0.05, 0.1), (0.01, 0.03) }
            .Select(
                (shape, i) =>
                    (int)
                        Analyze(
                            $"{i}.wav",
                            Music(shape.Item1, shape.Item2)
                        ).DynamicRangeAnalysis!.Value.CompressionSeverity
            )
            .ToList();

        Assert.Equal(severities.OrderDescending(), severities);
        Assert.True(severities.Distinct().Count() >= 3);
    }

    [Fact]
    public void Audio_OfSilence()
    {
        var result = Analyze(
            "silence.wav",
            SongFolder.Wave(0.5, SongFolder.Sine(0), SongFolder.Sine(0))
        );

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("Balanced", result.ChannelAnalysis!.Value.LouderChannel);
    }

    [Fact]
    public void Audio_OfAnMp3()
    {
        // Silent frames of 128 kbps.
        var frame = new byte[417];
        new byte[] { 0xFF, 0xFB, 0x90, 0x44 }.CopyTo(frame, 0);
        var result = Analyze(
            "audio.mp3",
            Enumerable.Repeat(frame, 100).SelectMany(bytes => bytes).ToArray()
        );

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("MP3", result.FormatAnalysis!.Value.Format);
        Assert.Equal("MPEG Audio Layer III", result.FormatAnalysis.Value.Codec);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    [InlineData(8192)]
    [InlineData(16384)]
    [InlineData(32768)]
    public void Audio_FrequenciesOfEverySize(int size)
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write(
            "audio.wav",
            SongFolder.Wave(1, SongFolder.Sine(0.8, 2000), SongFolder.Sine(0.8, 2000))
        );

        var frequency = AudioAnalysisService.GetFrequencyAnalysis(song.Path, "audio.wav", size);

        Assert.Equal(size / 2, frequency.FftData.Count());

        var spectrum = AudioAnalysisService.GetSpectralAnalysis(song.Path, "audio.wav", size, 100);

        Assert.NotEmpty(spectrum.SpectrogramData);
    }

    [Fact]
    public void AudioStream_NeedsAFilenameInTheFile()
    {
        song.Write("audio.mp3", "content");
        song.Write("a.osu", "osu file format v14\n[General]\n[Editor]\nAudioFilename: audio.mp3");
        Assert.Null(AudioAnalysisService.ResolveAudioStreamPath(song.Path));

        song.Write("a.osu", "osu file format v14");
        Assert.Null(AudioAnalysisService.ResolveAudioStreamPath(song.Path));
    }
}
