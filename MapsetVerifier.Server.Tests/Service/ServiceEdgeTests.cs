using System.Buffers.Binary;
using System.Text;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using MapsetVerifier.Snapshots.Store;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

public sealed class ServiceEdgeTests : IDisposable
{
    private readonly SongFolder song = new();

    public void Dispose() => song.Dispose();

    /// <summary> An FLV file carrying nothing but the given properties. </summary>
    private static byte[] Flv(params (string Name, double Value)[] properties)
    {
        static byte[] Name(string name) => [0, (byte)name.Length, .. Encoding.ASCII.GetBytes(name)];

        static byte[] Number(double value)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteDoubleBigEndian(bytes, value);

            return bytes;
        }

        byte[] script =
        [
            2,
            .. Name("onMetaData"),
            3,
            .. properties.SelectMany(p => (byte[])[.. Name(p.Name), 0, .. Number(p.Value)]),
            0,
            0,
            9,
        ];

        byte[] tag = [0, 0, 0, 0, 18, 0, 0, (byte)script.Length, 0, 0, 0, 0, 0, 0, 0, .. script];

        return [.. "FLV"u8, 1, 1, 0, 0, 0, 9, .. tag];
    }

    [Theory]
    [InlineData(59.4, "0:59")]
    [InlineData(65, "1:05")]
    [InlineData(7265, "2:01:05")]
    public void Videos_ShowTheirLengthAndSize(double seconds, string expected)
    {
        song.Write("a.osu", SongFolder.Osu(events: ["Video,100,\"video.flv\""]));
        song.Write(
            "video.flv",
            [
                .. Flv(
                    ("width", 640),
                    ("height", 480),
                    ("duration", seconds),
                    ("framerate", 29.97003),
                    ("videodatarate", 800),
                    ("videocodecid", 7)
                ),
                .. new byte[3 * 1024 * 1024],
            ]
        );

        var video = Assert.Single(VideoAnalysisService.AnalyzeVideos(song.Path).Videos);

        Assert.Equal(expected, video.DurationFormatted);
        Assert.Equal("3 MB", video.FileSizeFormatted);
        Assert.Equal("640 × 480", video.Resolution);
        Assert.Equal(29.97, video.FrameRate);
        Assert.Equal(800, video.VideoBitrateKbps);
        Assert.Equal(100, video.OffsetMs);
        // What the app can't play, it doesn't offer a preview of.
        Assert.Equal("H.264 / AVC", video.VideoCodec);
        Assert.False(video.CanPreview);
    }

    [Fact]
    public void Videos_WithoutAFrameRateOrBitrate_ShowNone()
    {
        song.Write("a.osu", SongFolder.Osu(events: ["Video,0,\"video.flv\""]));
        song.Write("video.flv", Flv(("width", 640), ("height", 480)));

        var video = Assert.Single(VideoAnalysisService.AnalyzeVideos(song.Path).Videos);

        Assert.Null(video.FrameRate);
        Assert.Null(video.VideoBitrateKbps);
        Assert.Null(
            Assert.Single(MetadataAnalysisService.Analyze(song.Path).Resources!.Videos).FrameRate
        );
    }

    [Fact]
    public void Storyboard_CountsEveryUse()
    {
        song.Write(
            "Artist - Title (Creator).osb",
            """
            [Events]
            Sprite,Background,Centre,"a.png",320,240
            Sprite,Background,Centre,"a.png",320,240
            Sprite,Foreground,Centre,"a.png",320,240
            Animation,Fail,Centre,"b.png",320,240,2,100,LoopForever
            Animation,Pass,Centre,"c.png",320,240,0,100,LoopForever
            Sample,1000,0,"s.wav",70
            Sample,500,0,"s.wav",70
            Sample,2000,0,"gone.wav",70
            """
        );
        song.Write("a.osu", SongFolder.Osu());
        song.Write("s.wav", "content");
        song.Write("b0.png", "not an image");

        var result = StoryboardDetailService.Analyze(song.Path);

        Assert.Equal((3, 2, 3), (result.SpriteCount, result.AnimationCount, result.SampleCount));

        var image = Assert.Single(result.Images, image => image.Path == "a.png");
        Assert.Equal(3, image.UseCount);
        Assert.Equal(["Background", "Foreground"], image.Layers);
        Assert.Equal(1, image.MissingFileCount);

        var animation = Assert.Single(result.Images, image => image.Path == "b.png");
        Assert.True(animation.IsAnimation);
        Assert.Equal(["b1.png"], animation.MissingFiles);

        // Without any frames, at least itself is missing.
        Assert.Equal(
            1,
            Assert.Single(result.Images, image => image.Path == "c.png").MissingFileCount
        );

        Assert.Equal([500, 1000, 2000], result.Samples.Select(sample => sample.Time));
        Assert.Equal(["s.wav", "gone.wav"], result.SampleFiles.Select(file => file.Path));
        Assert.Equal([true, false], result.SampleFiles.Select(file => file.Exists));
        Assert.Equal([2, 1], result.SampleFiles.Select(file => file.UseCount));
        Assert.Equal("7 B", result.SampleFiles[0].FileSizeFormatted);
    }

    [Fact]
    public void Snapshots_ThatAreUnknown_CannotBeCompared()
    {
        using var store = new SongFolder();
        SnapshotStore.ConfigurePath(store.Path, "externals");
        song.Write("a.osu", SongFolder.Osu(beatmapId: 5, beatmapSetId: 6));
        SnapshotService.Capture(song.Path, " ");

        var entry = Assert.Single(SnapshotStore.Load("set-6").Entries);
        Assert.Null(entry.Pin);

        foreach (var (first, second) in new[] { (entry.Id, "x"), ("x", entry.Id) })
        {
            Assert.Throws<ArgumentException>(() =>
                SnapshotService.GetWindow("set-6", first, second, "b-5", 0, 0, 0)
            );
        }
    }

    [Fact]
    public void Snapshots_OfNothing_AreNotTaken()
    {
        using var store = new SongFolder();
        SnapshotStore.ConfigurePath(store.Path, "externals");

        Assert.Empty(SnapshotService.Capture(song.Path, "Pin").Entries);
        Assert.False(Directory.Exists(Path.Combine(store.Path, "externals", "set-6")));
    }

    [Fact]
    public void Exceptions_WithoutACause_AreTheirOwn()
    {
        var exception = new AggregateException("nothing inside");

        Assert.Equal("nothing inside", ExceptionService.GetApiError(exception).Message);
        Assert.Contains(
            "nothing inside",
            ApiErrorFactory.FromException(exception, "Failed.").Details
        );
    }

    [Fact]
    public void Exceptions_DeeplyNested_AreUnwoundOnlySoFar()
    {
        Exception exception = new InvalidOperationException("innermost");

        for (var i = 0; i < 12; i++)
            exception = new AggregateException($"level {i}", exception);

        Assert.StartsWith("level 1", ExceptionService.GetApiError(exception).Message);
    }

    /// <summary> Only what's within the asked range is shown, which may be nothing. </summary>
    [Fact]
    public void Spectrum_OutsideOfAnyFrequency_IsEmpty()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write("audio.wav", SongFolder.Wave(0.5, SongFolder.Sine(0.8, 2000)));

        var spectrum = AudioAnalysisService.GetSpectralAnalysis(song.Path, "audio.wav");
        var peak = spectrum.PeakFrequencies.MaxBy(p => p.MagnitudeDb);

        Assert.Equal(2000, peak.FrequencyHz, 15d);
        Assert.True(spectrum.MaxDb > spectrum.MinDb);
        Assert.All(
            spectrum.SpectrogramData,
            frame => Assert.Equal(spectrum.FrequencyBins.Count(), frame.Magnitudes.Count())
        );
    }

    [Theory]
    [InlineData(440, "A4")]
    [InlineData(1760, "A6")]
    // Closest to a C, so of its octave, even if just below it.
    [InlineData(261, "C4")]
    [InlineData(262.3, "C4")]
    [InlineData(247, "B3")]
    public void Notes_AreNamed(double frequency, string expected)
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write("audio.wav", SongFolder.Wave(1, SongFolder.Sine(0.8, frequency)));

        var analysis = AudioAnalysisService.GetFrequencyAnalysis(song.Path, "audio.wav", 32768);

        var note = analysis.DetectedNotes.MaxBy(note => note.Confidence);

        Assert.Equal(expected, note.NoteName);
        Assert.Equal(frequency, note.FrequencyHz, (double)44100 / 32768);
    }

    /// <summary> A low sample rate has no high frequencies, and large steps may skip the bass. </summary>
    [Theory]
    [InlineData(8000, 4096)]
    [InlineData(96000, 256)]
    public void Frequencies_WithoutAnyInABand_AreStillAnalyzed(int sampleRate, int fftSize)
    {
        var wave = SongFolder.Wave(1, SongFolder.Sine(0.8, 2000));
        BinaryPrimitives.WriteInt32LittleEndian(wave.AsSpan(24), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(wave.AsSpan(28), sampleRate * 2);

        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write("audio.wav", wave);

        var analysis = AudioAnalysisService.GetFrequencyAnalysis(song.Path, "audio.wav", fftSize);
        var harmonics = analysis.HarmonicAnalysis;

        Assert.Equal(-100, sampleRate == 8000 ? harmonics.HighEnergy : harmonics.BassEnergy);
        // Played back at another rate, the tone is as much higher or lower.
        Assert.Equal(
            2000d * sampleRate / 44100,
            analysis.DominantFrequency,
            (double)sampleRate / fftSize
        );
    }

    [Fact]
    public void Kiai_ThatRepeatsOrNeverEnds_IsCountedOnce()
    {
        double Kiai(params string[] timing)
        {
            song.Write(
                "a.osu",
                SongFolder.Osu(
                    timingPoints: timing,
                    hitObjects: ["256,192,1000,5,0,0:0:0:0:", "256,192,10000,1,0,0:0:0:0:"]
                )
            );

            return BeatmapAnalysisService.Analyze(song.Path).Statistics.Single().KiaiTimeMs;
        }

        Assert.Equal(0, Kiai("0,500,4,2,0,100,1,0"));
        Assert.Equal(
            2000,
            Kiai(
                "0,500,4,2,0,100,1,0",
                "2000,-100,4,2,0,100,0,1",
                "3000,-100,4,2,0,100,0,1",
                "4000,-100,4,2,0,100,0,0",
                "5000,-100,4,2,0,100,0,0"
            )
        );
        // Until the last object.
        Assert.Equal(4000, Kiai("0,500,4,2,0,100,1,0", "6000,-100,4,2,0,100,0,1"));
        // And not at all past it.
        Assert.Equal(
            2000,
            Kiai(
                "0,500,4,2,0,100,1,0",
                "2000,-100,4,2,0,100,0,1",
                "4000,-100,4,2,0,100,0,0",
                "12000,-100,4,2,0,100,0,1"
            )
        );
    }

    private void Objects(params string[] hitObjects) =>
        song.Write("a.osu", SongFolder.Osu(hitObjects: hitObjects));

    /// <summary> Long stretches without hit sounds, of which both the length and objects count. </summary>
    [Theory]
    [InlineData(20, 1000, true)]
    [InlineData(60, 100, false)] // Many, but over a short time.
    [InlineData(4, 10000, true)]
    [InlineData(3, 10000, false)] // Long, but hardly any objects.
    [InlineData(12, 1000, false)] // Exactly the limit of 12 seconds.
    [InlineData(13, 1000, true)]
    public void HitSoundGaps_NeedTimeAndObjects(int objects, int step, bool expected)
    {
        Objects([
            "256,192,1000,5,2,0:0:0:0:",
            .. Enumerable
                .Range(1, objects - 1)
                .Select(i => $"256,192,{1000 + i * step},1,0,0:0:0:0:"),
            $"256,192,{1000 + objects * step},1,8,0:0:0:0:",
        ]);

        var gaps = BeatmapAnalysisService
            .AnalyzeObjects(song.Path)
            .Difficulties.Single()
            .HitsoundGapPeriods;

        Assert.Equal(expected, gaps.Count > 0);

        if (expected)
        {
            Assert.Equal(1000, gaps[0].StartTimeMs);
            Assert.Equal(1000 + objects * step, gaps[0].EndTimeMs);
        }
    }

    [Fact]
    public void HitSoundGaps_EndWithEveryHitSound()
    {
        Objects(
            Enumerable
                .Range(0, 60)
                .Select(i => $"256,192,{1000 + i * 1000},1,{(i % 10 == 0 ? 2 : 0)},0:0:0:0:")
                .ToArray()
        );

        Assert.Empty(
            BeatmapAnalysisService
                .AnalyzeObjects(song.Path)
                .Difficulties.Single()
                .HitsoundGapPeriods
        );
    }

    [Fact]
    public void TimingSegments_LastUntilTheNext_AndNeedALength()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                timingPoints:
                [
                    "0,500,4,2,0,100,1,0",
                    "2000,-100,4,2,0,100,0,0",
                    "4000,250,3,2,0,100,1,0",
                    "90000,300,4,2,0,100,1,0",
                ],
                hitObjects: ["256,192,1000,5,0,0:0:0:0:", "256,192,8000,1,0,0:0:0:0:"]
            )
        );

        var segments = BeatmapAnalysisService
            .AnalyzeObjects(song.Path)
            .Difficulties.Single()
            .TimingSegments;

        Assert.Equal([0, 4000], segments.Take(2).Select(segment => segment.StartTimeMs));
        Assert.Equal([4000, 90000], segments.Take(2).Select(segment => segment.EndTimeMs));
        Assert.Equal([500, 250], segments.Take(2).Select(segment => segment.MsPerBeat));
        Assert.Equal([4, 3], segments.Take(2).Select(segment => segment.Meter));
        Assert.All(segments, segment => Assert.True(segment.EndTimeMs > segment.StartTimeMs));
    }

    [Fact]
    public void StarRatingSamples_ReachTheEndOfTheLastObject()
    {
        Objects(
            "256,192,1000,5,0,0:0:0:0:",
            "100,192,2000,1,0,0:0:0:0:",
            "256,192,3000,12,0,20000,0:0:0:0:"
        );

        var samples = BeatmapAnalysisService
            .AnalyzeDifficulty(song.Path)
            .Difficulties.Single()
            .StarRatingSamples;

        Assert.InRange(samples.Max(sample => sample.TimeMs), 19500, 21000);
    }

    [Fact]
    public void Mania_WithoutKeys_HasNoColumns()
    {
        song.Write("a.osu", SongFolder.Osu(mode: 3, circleSize: "0"));
        Assert.Null(
            BeatmapAnalysisService.AnalyzeObjects(song.Path).Difficulties.Single().ColumnUsage
        );

        song.Write("a.osu", SongFolder.Osu(mode: 3, circleSize: "4"));
        Assert.NotEmpty(
            BeatmapAnalysisService.AnalyzeObjects(song.Path).Difficulties.Single().ColumnUsage!
        );
    }

    [Fact]
    public void ChannelBalance_OfSilence_IsEven()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write(
            "audio.wav",
            SongFolder.Wave(1, SongFolder.Sine(0), time => time < 0.5 ? 0 : 0.5)
        );

        var balance = AudioAnalysisService
            .AnalyzeAudio(song.Path)
            .ChannelAnalysis!.Value.BalanceOverTime.ToList();

        Assert.Equal(0, balance[0].Balance);
        Assert.Equal(1, balance[^1].Balance);
        Assert.Equal([0, 100, 200], balance.Take(3).Select(point => point.TimeMs));
    }

    /// <summary> Each is louder than both of its neighbours, and loud enough to be heard at all. </summary>
    [Fact]
    public void PeakFrequencies_AreLocalMaxima()
    {
        var random = new Random(3);
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write(
            "audio.wav",
            SongFolder.Wave(
                1,
                time =>
                    SongFolder.Sine(0.4, 2000)(time)
                    + SongFolder.Sine(0.2, 5000)(time)
                    + (random.NextDouble() - 0.5) * 0.02
            )
        );

        var spectrum = AudioAnalysisService.GetSpectralAnalysis(song.Path, "audio.wav", 4096, 100);
        var frames = spectrum.SpectrogramData.Select(f => f.Magnitudes.ToArray()).ToList();
        var average = Enumerable
            .Range(0, frames[0].Length)
            .Select(i => frames.Average(frame => (double)frame[i]))
            .ToArray();
        var bins = spectrum.FrequencyBins.ToList();
        var peaks = spectrum.PeakFrequencies.ToList();

        Assert.Equal(
            [2000, 5000],
            peaks.Take(2).Select(peak => Math.Round(peak.FrequencyHz / 100) * 100)
        );
        Assert.All(
            peaks,
            peak =>
            {
                var i = bins.IndexOf(peak.FrequencyHz);

                Assert.InRange(peak.FrequencyHz, 20, 22050);
                Assert.True(average[i] > -60);
                Assert.True(i == 0 || average[i] > average[i - 1]);
                Assert.True(i == average.Length - 1 || average[i] > average[i + 1]);
            }
        );
    }

    [Fact]
    public void PeakLevel_IsOfTheLouderChannel()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write("audio.wav", SongFolder.Wave(1, SongFolder.Sine(0.2), SongFolder.Sine(0.8)));

        var result = AudioAnalysisService.AnalyzeAudio(song.Path);

        Assert.Equal(20 * Math.Log10(0.8), result.DynamicRangeAnalysis!.Value.PeakLevel, 0.05);
        Assert.Equal("Right", result.ChannelAnalysis!.Value.LouderChannel);
        Assert.Equal(4, result.ChannelAnalysis.Value.BalanceRatio, 0.1);
    }

    [Fact]
    public void Spectrum_OfAMissingFile_IsEmpty()
    {
        song.Write("a.osu", SongFolder.Osu());

        var spectrum = AudioAnalysisService.GetSpectralAnalysis(song.Path, "missing.wav");

        Assert.Empty(spectrum.SpectrogramData);
        Assert.Empty(spectrum.PeakFrequencies);
        Assert.Empty(AudioAnalysisService.GetFrequencyAnalysis(song.Path, "missing.wav").FftData);
    }

    /// <summary> Three slides of 500 ms, each end with a hit normal of its own. </summary>
    [Fact]
    public void EverySliderEdge_HasItsHitNormal()
    {
        Objects(
            "100,100,1000,6,0,L|240:100,3,140,0|2|8|4,1:0|2:0|3:0|1:0,0:0:0:0:",
            "100,300,9000,1,0,0:0:0:0:"
        );

        var normals = BeatmapAnalysisService
            .AnalyzeObjects(song.Path)
            .Difficulties.Single()
            .TimelineSamples.Where(sample => sample.IsBaseHitNormal && sample.TimeMs < 5000)
            .ToList();

        Assert.Equal([1000, 1500, 2000, 2500], normals.Select(sample => Math.Round(sample.TimeMs)));
        Assert.Equal(
            ["Normal", "Soft", "Drum", "Normal"],
            normals.Select(sample => sample.Sampleset)
        );
    }

    /// <summary> As long as the longest difficulty needs, so that all of them share their steps. </summary>
    [Fact]
    public void DifficultySteps_AreThoseOfTheLongest()
    {
        song.Write("a.osu", SongFolder.Osu("Short"));
        Assert.Equal(400, BeatmapAnalysisService.AnalyzeDifficulty(song.Path).MsPerPeak);

        song.Write(
            "b.osu",
            SongFolder.Osu(
                "Long",
                hitObjects: ["256,192,1000,5,0,0:0:0:0:", "256,192,7200000,1,0,0:0:0:0:"]
            )
        );
        Assert.True(BeatmapAnalysisService.AnalyzeDifficulty(song.Path).MsPerPeak > 400);
    }

    [Fact]
    public void Metadata_HasTheAudioFileAndStoryboard()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(general: ["AudioFilename: audio.wav"], events: ["Video,0,\"video.flv\""])
        );
        song.Write(
            "Artist - Title (Creator).osb",
            """
            [Events]
            Video,500,"video.flv"
            Video,500,"other.flv"
            Sprite,Background,Centre,"a.png",320,240
            """
        );
        song.Write("audio.wav", SongFolder.Wave(2, SongFolder.Sine(0.5), SongFolder.Sine(0.5)));
        song.Write("video.flv", Flv(("width", 640), ("height", 480), ("framerate", 25)));

        var resources = MetadataAnalysisService.Analyze(song.Path).Resources!;

        Assert.Equal("audio.wav", resources.AudioFile!.FileName);
        Assert.Equal(2000, resources.AudioFile.DurationMs, 1d);
        // The same one only once.
        Assert.Equal(["other.flv", "video.flv"], resources.Videos.Select(v => v.FileName).Order());
        Assert.Equal(25, resources.Videos.Single(v => v.FileName == "video.flv").FrameRate);

        Assert.True(BeatmapAnalysisService.Analyze(song.Path).Success);
        Assert.Equal(2000, StoryboardDetailService.Analyze(song.Path).SongDurationMs, 1d);
    }

    [Fact]
    public void Samples_AtTheVeryStart_ReplaceTheFirst()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                timingPoints:
                [
                    "0,500,4,2,0,100,1,0",
                    "0,-50,4,2,0,40,0,0",
                    "4000,-100,4,2,0,80,0,0",
                ]
            )
        );

        var difficulty = BeatmapAnalysisService.AnalyzeDifficulty(song.Path).Difficulties.Single();

        Assert.Equal([0, 4000], difficulty.VolumeSamples.Select(s => s.TimeMs).Distinct().Take(2));
        Assert.Equal(40, difficulty.VolumeSamples[0].Value);
        Assert.Equal(2, difficulty.SliderVelocitySamples[0].Value);
    }

    [Fact]
    public void Columns_LeaveOutObjectsBesideThem()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                mode: 3,
                circleSize: "4",
                hitObjects:
                [
                    "64,192,1000,1,0,0:0:0:0:",
                    "448,192,1000,128,0,2000:0:0:0:0:",
                    "-600,192,3000,1,0,0:0:0:0:",
                    "2000,192,4000,1,0,0:0:0:0:",
                ]
            )
        );

        var result = BeatmapAnalysisService.AnalyzeObjects(song.Path);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(4, result.Difficulties.Single().ColumnUsage!.Count);
    }

    [Theory]
    [InlineData(0.5, 0.5, "None", "Balanced")]
    [InlineData(0.5, 0.35, "None", "Left")] // 1.43 times as loud.
    [InlineData(0.5, 0.32, "Minor", "Left")] // 1.56 times.
    [InlineData(0.3, 0.5, "Minor", "Right")]
    [InlineData(0.5, 0.26, "Minor", "Left")] // 1.92 times.
    [InlineData(0.5, 0.24, "Warning", "Left")] // 2.08 times.
    [InlineData(0.5, 0, "Severe", "Left (Right silent)")]
    [InlineData(0, 0.5, "Severe", "Right (Left silent)")]
    public void Imbalance_IsRatedByHowMuchLouderOneSideIs(
        double left,
        double right,
        string severity,
        string louder
    )
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write("audio.wav", SongFolder.Wave(0.5, _ => left, _ => right));

        var channels = AudioAnalysisService.AnalyzeAudio(song.Path).ChannelAnalysis!.Value;

        Assert.Equal(severity, channels.Severity.ToString());
        Assert.Equal(louder, channels.LouderChannel);
        Assert.Equal(2, channels.ChannelCount);
        Assert.True(channels.IsStereo);
        Assert.False(channels.IsMono);
    }

    [Fact]
    public void Mono_HasNoWidth_AndIsBalanced()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.wav"]));
        song.Write("audio.wav", SongFolder.Wave(0.5, SongFolder.Sine(0.5)));

        var channels = AudioAnalysisService.AnalyzeAudio(song.Path).ChannelAnalysis!.Value;

        Assert.True(channels.IsMono);
        Assert.Equal(0, channels.StereoWidth);
        Assert.Equal("None", channels.Severity.ToString());
    }

    /// <summary> As big endian as everything in it. </summary>
    private static byte[] Aiff(int frames)
    {
        static byte[] U32(int value) =>
            [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

        byte[] common =
        [
            .. "COMM"u8,
            .. U32(18),
            0,
            1, // Channels.
            .. U32(frames),
            0,
            16, // Bits.
            0x40,
            0x0E,
            0xAC,
            0x44,
            0,
            0,
            0,
            0,
            0,
            0, // 44100 as 80 bit float.
        ];
        byte[] sound =
        [
            .. "SSND"u8,
            .. U32(8 + frames * 2),
            .. U32(0),
            .. U32(0),
            .. new byte[frames * 2],
        ];

        return
        [
            .. "FORM"u8,
            .. U32(4 + common.Length + sound.Length),
            .. "AIFF"u8,
            .. common,
            .. sound,
        ];
    }

    [Fact]
    public void OtherFormats_AreNamed()
    {
        song.Write("a.osu", SongFolder.Osu(general: ["AudioFilename: audio.aiff"]));
        song.Write("audio.aiff", Aiff(22050));

        var format = AudioAnalysisService.AnalyzeAudio(song.Path).FormatAnalysis!.Value;

        Assert.Equal("AIFF", format.Format);
        Assert.Equal("Audio Interchange File Format", format.Codec);
    }

    [Theory]
    [InlineData("MP4", "H.264 / AVC", true)]
    [InlineData("MP4", "H.265 / HEVC", true)]
    [InlineData("MP4", "VP8", true)]
    [InlineData("MP4", "VP9", true)]
    [InlineData("WEBM", "AV1", true)]
    [InlineData("MP4", "MPEG-4 Visual", false)]
    [InlineData("MP4", "", false)]
    [InlineData("AVI", "H.264 / AVC", false)]
    [InlineData("", "H.264 / AVC", false)]
    public void Videos_ThatCanBePreviewed(string container, string codec, bool expected)
    {
        Assert.Equal(expected, VideoAnalysisService.CanBrowserPlay(container, codec));
    }

    [Theory]
    [InlineData(new[] { -20.0, -22, -24 }, true)]
    [InlineData(new[] { -10.0, -30, -30 }, false)] // One stands out.
    [InlineData(new[] { -50.0, -51, -52 }, false)] // Too quiet to be heard.
    public void Masking_NeedsLoudnessAllOverABand(double[] magnitudes, bool expected)
    {
        var points = magnitudes
            .Select(
                (magnitude, i) =>
                    new Model.AudioAnalysis.FftDataPoint
                    {
                        FrequencyHz = 100 + i,
                        MagnitudeDb = magnitude,
                    }
            )
            .ToList();

        var results = AudioAnalysisService.DetectFrequencyMasking(points);

        Assert.Equal(expected, results.Count == 1);
        Assert.All(results, result => Assert.Contains("Bass", result.Description));
    }

    [Fact]
    public void Snapshots_RememberStarsThatWereMissing()
    {
        using var store = new SongFolder();
        SnapshotStore.ConfigurePath(store.Path, "externals");

        for (var i = 0; i < 2; i++)
        {
            song.Write(
                "a.osu",
                SongFolder.Osu(
                    title: $"{i}",
                    beatmapId: 5,
                    beatmapSetId: 6,
                    hitObjects: ["0,0,1000,1,0", "500,0,1200,1,0", $"0,{i},1400,1,0"]
                )
            );
            SnapshotService.Capture(song.Path, null);
        }

        // Like those of older versions.
        var log = Directory
            .GetFiles(store.Path, "*.json", SearchOption.AllDirectories)
            .Single(file =>
                File.ReadAllText(file).Contains("\"stars\"", StringComparison.OrdinalIgnoreCase)
            );
        File.WriteAllText(
            log,
            System.Text.RegularExpressions.Regex.Replace(
                File.ReadAllText(log),
                "(\"stars\"):\\s*[0-9.]+",
                "$1: null",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            )
        );

        double?[] Stars() =>
            SnapshotStore
                .Load("set-6")
                .Entries.SelectMany(entry => entry.Difficulties)
                .Select(difficulty => difficulty.Stars)
                .ToArray();

        Assert.Equal([null, null], Stars());

        var entries = SnapshotStore.Load("set-6").Entries;
        var compared = Assert.Single(
            SnapshotService.Compare("set-6", entries[0].Id, entries[1].Id).Difficulties
        );

        Assert.Equal([compared.StarsBefore, compared.StarsAfter], Stars());
        Assert.All(Stars(), stars => Assert.True(stars > 0));
    }

    [Fact]
    public void Snapshots_OfNoDifficulties_AreNotTaken() =>
        Assert.Null(
            SnapshotService.CaptureCurrent(
                new MapsetVerifier.Parser.Objects.BeatmapSet(song.Path),
                "check"
            )
        );
}
