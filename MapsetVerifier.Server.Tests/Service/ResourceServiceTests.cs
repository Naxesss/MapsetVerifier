using MapsetVerifier.Server.Model.AudioAnalysis;
using MapsetVerifier.Server.Service;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

/// <summary> Metadata, storyboard, video and audio of a mapset, and which of its files are served. </summary>
public sealed class ResourceServiceTests : IDisposable
{
    private readonly SongFolder song = new();

    public void Dispose() => song.Dispose();

    private static byte[] Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);

        return stream.ToArray();
    }

    [Fact]
    public void EmptyFolder_IsAnError()
    {
        Assert.False(MetadataAnalysisService.Analyze(song.Path).Success);
        Assert.False(VideoAnalysisService.AnalyzeVideos(song.Path).Success);
        Assert.False(AudioAnalysisService.AnalyzeAudio(song.Path).Success);
        Assert.False(
            StoryboardDetailService.Analyze(System.IO.Path.Combine(song.Path, "missing")).Success
        );
    }

    [Fact]
    public void Metadata_OfEachDifficulty()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu("Hard", artist: "A", title: "T", beatmapId: 12, beatmapSetId: 34)
        );

        var difficulty = Assert.Single(MetadataAnalysisService.Analyze(song.Path).Difficulties);

        Assert.Equal("Hard", difficulty.Version);
        Assert.Equal("A", difficulty.Artist);
        Assert.Equal("A", difficulty.ArtistUnicode);
        Assert.Equal("T", difficulty.Title);
        Assert.Equal(12ul, difficulty.BeatmapId);
        Assert.Equal(34ul, difficulty.BeatmapSetId);
        Assert.Equal("Standard", difficulty.Mode);
    }

    [Fact]
    public void HitSounds_ListWhereTheyAreUsed()
    {
        string[] Claps(int count) =>
            Enumerable
                .Range(0, count)
                .Select(i => $"256,192,{1000 + i * 500},1,8,0:0:0:0:")
                .ToArray();

        song.Write(
            "a.osu",
            SongFolder.Osu("A", timingPoints: ["0,500,4,2,2,100,1,0"], hitObjects: Claps(12))
        );
        song.Write("b.osu", SongFolder.Osu("B"));
        song.Write("soft-hitclap2.wav", SongFolder.Wave(0.2, SongFolder.Sine(0.5)));

        var usage = Assert.Single(MetadataAnalysisService.Analyze(song.Path).Resources.HitSounds);

        Assert.Equal("soft-hitclap2.wav", usage.FileName);
        Assert.Equal(12, usage.TotalUsageCount);
        Assert.Equal(200, usage.DurationMs, 1);
        var difficulty = Assert.Single(usage.UsagePerDifficulty);
        Assert.Equal("A", difficulty.Version);
        Assert.Equal(10, difficulty.Timestamps.Count); // Only the first few.
        Assert.StartsWith("00:01:000", difficulty.Timestamps[0]);
    }

    [Fact]
    public void BrokenHitSound_IsStillListed()
    {
        song.Write("a.osu", SongFolder.Osu(timingPoints: ["0,500,4,2,2,100,1,0"]));
        song.Write("soft-hitnormal2.wav", "not audio");

        var usage = Assert.Single(MetadataAnalysisService.Analyze(song.Path).Resources.HitSounds);

        Assert.Equal(9, usage.FileSizeBytes);
        Assert.Equal(2, usage.TotalUsageCount);
    }

    [Fact]
    public void Backgrounds_ListTheirResolutionAndDifficulties()
    {
        song.Write("a.osu", SongFolder.Osu("A", events: ["0,0,\"bg.png\",0,0"]));
        song.Write("b.osu", SongFolder.Osu("B", events: ["0,0,\"bg.png\",0,0"]));
        song.Write("c.osu", SongFolder.Osu("C", events: ["0,0,\"gone.png\",0,0"]));
        song.Write("bg.png", Png(320, 180));

        var backgrounds = MetadataAnalysisService
            .Analyze(song.Path)
            .Resources.Backgrounds.ToDictionary(background => background.FileName);

        Assert.Equal("320 × 180", backgrounds["bg.png"].Resolution);
        Assert.Equal(["A", "B"], backgrounds["bg.png"].UsedByDifficulties.Order());
        Assert.Equal(0, backgrounds["gone.png"].Width);
    }

    [Fact]
    public void FolderSize_IncludesSubfolders()
    {
        var osu = new FileInfo(song.Write("a.osu", SongFolder.Osu())).Length;
        song.Write("a.bin", new byte[1000]);
        song.Write("sb/b.bin", new byte[2000]);

        Assert.Equal(
            osu + 3000,
            MetadataAnalysisService.Analyze(song.Path).Resources.TotalFolderSizeBytes
        );
    }

    [Fact]
    public void Colours_AreInTheOrderTheEditorShows()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                "Standard",
                colours:
                [
                    "Combo1 : 255,255,255",
                    "Combo2 : 10,20,30",
                    "Combo3 : 100,100,100",
                    "SliderBorder : 42,42,42",
                ]
            )
        );
        song.Write("b.osu", SongFolder.Osu("Taiko", mode: 1, colours: ["Combo1 : 1,2,3"]));

        var settings = MetadataAnalysisService
            .Analyze(song.Path)
            .ColourSettings.ToDictionary(s => s.Version);
        var standard = settings["Standard"];

        Assert.Equal([1, 2, 3], standard.ComboColours.Select(colour => colour.Index));
        Assert.Equal(
            ["#0A141E", "#646464", "#FFFFFF"],
            standard.ComboColours.Select(colour => colour.Hex)
        );
        Assert.Contains("Too dark", standard.ComboColours[0].LuminosityWarning);
        Assert.Equal("", standard.ComboColours[1].LuminosityWarning);
        Assert.Contains("Too bright", standard.ComboColours[2].LuminosityWarning);
        Assert.Contains("Too dark", standard.SliderBorder!.LuminosityWarning);
        Assert.Null(standard.SliderTrack);

        Assert.False(settings["Taiko"].IsApplicable);
        Assert.Empty(settings["Taiko"].ComboColours);
    }

    [Fact]
    public void Storyboard_IsSummarisedByLayerAndSource()
    {
        song.Write("a.osu", SongFolder.Osu("A", events: ["Sprite,Pass,Centre,\"a.png\",0,0"]));
        song.Write("b.osu", SongFolder.Osu("B"));
        song.Write(
            "Artist - Title (Creator).osb",
            """
            [Events]
            Sprite,Foreground,Centre,"sb\a.png",0,0
            Sprite,Background,Centre,"sb\a.png",0,0
            Animation,Foreground,Centre,"sb\run.png",0,0,3,100,LoopForever
            Sample,2000,0,"hit.wav",60
            Sample,1000,0,"hit.wav",70
            """
        );
        song.Write("sb/a.png", Png(64, 32));
        song.Write("sb/run1.png", Png(16, 16));
        song.Write("hit.wav", new byte[500]);

        var result = StoryboardDetailService.Analyze(song.Path);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(3, result.SpriteCount);
        Assert.Equal(1, result.AnimationCount);
        Assert.Equal(2, result.SampleCount);
        Assert.Equal(
            [("Background", 1), ("Pass", 1), ("Foreground", 2)],
            result.Layers.Select(layer => (layer.Layer, layer.Count))
        );
        Assert.Equal(
            ["Artist - Title (Creator).osb", "A"],
            result.Sources.Select(source => source.Name)
        );
        // A used .osb shows everywhere.
        Assert.Equal(["A", "B"], result.UsedByDifficulties.Order());

        var sprite = Assert.Single(result.Images, image => image.Path == "sb/a.png");
        Assert.Equal(2, sprite.UseCount);
        Assert.Equal("64 × 32", sprite.Resolution);
        Assert.Equal(["Background", "Foreground"], sprite.Layers);

        var animation = Assert.Single(result.Images, image => image.IsAnimation);
        Assert.Equal(3, animation.FrameCount);
        Assert.Equal(["sb/run0.png", "sb/run2.png"], animation.MissingFiles);
        Assert.Equal("sb/run1.png", animation.PreviewPath);

        // Images with missing files come first.
        Assert.Equal("sb/a.png", result.Images[^1].Path);
        Assert.Equal(2, result.ImageFileCount);

        Assert.Equal([1000, 2000], result.Samples.Select(sample => sample.Time));
        var file = Assert.Single(result.SampleFiles);
        Assert.Equal(2, file.UseCount);
        Assert.Equal(500, result.SampleSizeBytes);
    }

    [Theory]
    [InlineData("bg.png", true)]
    [InlineData("BG.PNG", true)]
    [InlineData("sb/run1.png", true)]
    [InlineData("hit.wav", true)]
    [InlineData("secret.txt", false)] // Not part of the beatmap.
    [InlineData("sb/run9.png", false)] // Past the last frame.
    [InlineData("gone.png", false)] // Used, but missing.
    [InlineData("../outside.png", false)] // Used, but outside of the folder.
    public void ResourceFiles_AreOnlyServedIfUsedAndWithinTheFolder(string name, bool expected)
    {
        using var library = new SongFolder();
        library.Write("outside.png", Png(1, 1));
        library.Write(
            "set/a.osu",
            SongFolder.Osu(
                events:
                [
                    "0,0,\"bg.png\",0,0",
                    "Sprite,Pass,Centre,\"gone.png\",0,0",
                    "Sprite,Pass,Centre,\"../outside.png\",0,0",
                    "Animation,Pass,Centre,\"sb\\run.png\",0,0,2,100,LoopForever",
                    "Sample,0,0,\"hit.wav\",60",
                ]
            )
        );
        library.Write("set/bg.png", Png(1, 1));
        library.Write("set/sb/run1.png", Png(1, 1));
        library.Write("set/hit.wav");
        library.Write("set/secret.txt");

        // The case of names only differs where the file system doesn't care.
        if (name == "BG.PNG" && !OperatingSystem.IsWindows())
            return;

        var path = MetadataAnalysisService.ResolveResourceFilePath(
            System.IO.Path.Combine(library.Path, "set"),
            name
        );

        Assert.Equal(expected, path != null);
    }

    [Fact]
    public void Videos_AreListedOnce_WithWhatUsesThem()
    {
        song.Write("a.osu", SongFolder.Osu("A", events: ["Video,-100,\"v.mp4\""]));
        song.Write("b.osu", SongFolder.Osu("B", events: ["Video,-100,\"V.mp4\""]));
        song.Write("Artist - Title (Creator).osb", "[Events]\nVideo,50,\"v.mp4\"");

        var video = Assert.Single(VideoAnalysisService.AnalyzeVideos(song.Path).Videos);

        Assert.False(video.Exists);
        Assert.Equal(-100, video.OffsetMs);
        Assert.Equal(["(Storyboard)", "A", "B"], video.UsedByDifficulties.Order());
    }

    [Fact]
    public void VideoFiles_AreOnlyServedIfReferencedAndWithinTheFolder()
    {
        using var library = new SongFolder();
        library.Write("outside.mp4");
        library.Write(
            "set/a.osu",
            SongFolder.Osu(events: ["Video,0,\"v.mp4\"", "Video,0,\"../outside.mp4\""])
        );
        library.Write("set/v.mp4");
        library.Write("set/other.mp4");
        var folder = System.IO.Path.Combine(library.Path, "set");

        Assert.NotNull(VideoAnalysisService.ResolveReferencedVideoPath(folder, "v.mp4"));
        Assert.Null(VideoAnalysisService.ResolveReferencedVideoPath(folder, "other.mp4"));
        Assert.Null(VideoAnalysisService.ResolveReferencedVideoPath(folder, "../outside.mp4"));
    }

    [Fact]
    public void AudioFile_IsOnlyServedFromWithinTheFolder()
    {
        using var library = new SongFolder();
        library.Write("outside.mp3");
        library.Write("set/a.osu", SongFolder.Osu(general: ["AudioFilename: ../outside.mp3"]));
        var folder = System.IO.Path.Combine(library.Path, "set");

        Assert.Null(AudioAnalysisService.ResolveAudioStreamPath(folder));

        library.Write("set/b.osu", SongFolder.Osu(general: ["AudioFilename: song.mp3"]));
        Assert.Null(AudioAnalysisService.ResolveAudioStreamPath(folder));

        var audio = library.Write("set/song.mp3");
        Assert.Equal(audio, AudioAnalysisService.ResolveAudioStreamPath(folder));
        Assert.Null(
            AudioAnalysisService.ResolveAudioStreamPath(
                System.IO.Path.Combine(library.Path, "missing")
            )
        );
    }

    private AudioAnalysisResult AnalyzeAudio(params Func<double, double>[] channels)
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: song.wav"]));
        song.Write("song.wav", SongFolder.Wave(1, channels));
        var result = AudioAnalysisService.AnalyzeAudio(song.Path);
        Assert.True(result.Success, result.ErrorMessage);

        return result;
    }

    [Fact]
    public void Audio_FormatAndLevels()
    {
        var result = AnalyzeAudio(SongFolder.Sine(0.5), SongFolder.Sine(0.5));

        Assert.Equal("song.wav", result.AudioFilePath);
        Assert.Equal(44100, result.FormatAnalysis!.Value.SampleRate);
        Assert.True(result.FormatAnalysis.Value.IsStandardSampleRate);
        Assert.Equal("0:01", result.FormatAnalysis.Value.DurationFormatted);
        Assert.Equal(2, result.FormatAnalysis.Value.Channels);
        Assert.Equal(1411, result.BitrateAnalysis!.Value.AverageBitrate);
        // Half of full scale is 6 dB below it.
        Assert.Equal(-6.02, result.DynamicRangeAnalysis!.Value.PeakLevel, 0.1);
        Assert.False(result.DynamicRangeAnalysis.Value.ClippingDetected);
    }

    [Fact]
    public void Audio_NameOfAWaveFile()
    {
        Assert.Equal("WAV", AnalyzeAudio(SongFolder.Sine(0.5)).FormatAnalysis!.Value.Format);
    }

    [Fact]
    public void Audio_Clipping()
    {
        Assert.True(
            AnalyzeAudio(
                SongFolder.Sine(1),
                SongFolder.Sine(0.2)
            ).DynamicRangeAnalysis!.Value.ClippingDetected
        );
    }

    [Theory]
    [InlineData(0.5, 0.5, "Balanced", ImbalanceSeverity.None)]
    [InlineData(0.5, 0.4, "Left", ImbalanceSeverity.None)]
    [InlineData(0.3, 0.5, "Right", ImbalanceSeverity.Minor)]
    [InlineData(0.8, 0.3, "Left", ImbalanceSeverity.Warning)]
    [InlineData(0.5, 0, "Left (Right silent)", ImbalanceSeverity.Severe)]
    [InlineData(0, 0.5, "Right (Left silent)", ImbalanceSeverity.Severe)]
    public void Audio_ChannelBalance(
        double left,
        double right,
        string louder,
        ImbalanceSeverity severity
    )
    {
        var channels = AnalyzeAudio(
            SongFolder.Sine(left),
            SongFolder.Sine(right)
        ).ChannelAnalysis!.Value;

        Assert.Equal(louder, channels.LouderChannel);
        Assert.Equal(severity, channels.Severity);
        Assert.True(channels.IsStereo);
        Assert.Equal(10, channels.BalanceOverTime.Count());
    }

    [Fact]
    public void Audio_OppositePhase_IsNegativelyCorrelated()
    {
        var sine = SongFolder.Sine(0.5);

        Assert.True(
            AnalyzeAudio(sine, time => -sine(time)).ChannelAnalysis!.Value.PhaseCorrelation < 0
        );
    }

    [Fact]
    public void Audio_Mono()
    {
        var channels = AnalyzeAudio(SongFolder.Sine(0.5)).ChannelAnalysis!.Value;

        Assert.True(channels.IsMono);
        Assert.False(channels.IsStereo);
        Assert.Equal(0, channels.StereoWidth);
        Assert.Equal("Balanced", channels.LouderChannel);
    }

    [Fact]
    public void Audio_DominantFrequency()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: song.wav"]));
        song.Write("song.wav", SongFolder.Wave(1, SongFolder.Sine(0.5, 440)));

        var frequencies = AudioAnalysisService.GetFrequencyAnalysis(song.Path, fftSize: 8192);

        Assert.Equal(440, frequencies.DominantFrequency, 6d);
    }

    [Fact]
    public void Audio_Spectrogram_IsWithinHearing()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: song.wav"]));
        song.Write("song.wav", SongFolder.Wave(0.5, SongFolder.Sine(0.5, 440)));

        var spectrum = AudioAnalysisService.GetSpectralAnalysis(
            song.Path,
            fftSize: 1024,
            timeResolutionMs: 100
        );

        Assert.All(spectrum.FrequencyBins, bin => Assert.InRange(bin, 20, 22050));
        Assert.Equal(5, spectrum.SpectrogramData.Count());
        Assert.All(
            spectrum.SpectrogramData,
            frame => Assert.Equal(spectrum.FrequencyBins.Count(), frame.Magnitudes.Count())
        );
        Assert.True(spectrum.MinDb < spectrum.MaxDb);
    }

    [Fact]
    public void Audio_ThatIsMissing_IsEmptyOrAnError()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: song.wav"]));

        Assert.Equal(
            "Audio file not found in beatmap set.",
            AudioAnalysisService.AnalyzeAudio(song.Path).ErrorMessage
        );
        Assert.Empty(AudioAnalysisService.GetSpectralAnalysis(song.Path).SpectrogramData);
        Assert.Empty(AudioAnalysisService.GetFrequencyAnalysis(song.Path).FftData);
    }
}
