using ManagedBass;
using MapsetVerifier.Framework.Objects.Resources;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources;

/// <summary> Decodes generated wave files, so what comes out is known exactly. </summary>
public sealed class AudioDecodingTests : IDisposable
{
    private const int SampleRate = 44100;

    private readonly string folder = Path.Combine(
        Path.GetTempPath(),
        "MapsetVerifierFrameworkTests",
        Guid.NewGuid().ToString("N")
    );

    public AudioDecodingTests() => Directory.CreateDirectory(folder);

    public void Dispose()
    {
        try
        {
            Directory.Delete(folder, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary> Writes 16 bit PCM, with each channel's sample given by its time in seconds. </summary>
    private string Wave(string name, double seconds, params Func<double, double>[] channels)
    {
        var path = Path.Combine(folder, name);
        var frames = (int)(seconds * SampleRate);
        var dataSize = frames * channels.Length * 2;

        using var writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels.Length);
        writer.Write(SampleRate);
        writer.Write(SampleRate * channels.Length * 2);
        writer.Write((short)(channels.Length * 2));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataSize);

        for (var i = 0; i < frames; ++i)
            foreach (var channel in channels)
                writer.Write((short)Math.Round(channel((double)i / SampleRate) * short.MaxValue));

        return path;
    }

    private static Func<double, double> Sine(double amplitude, double frequency = 1000) =>
        time => amplitude * Math.Sin(2 * Math.PI * frequency * time);

    private static double Silence(double time) => 0;

    [Fact]
    public void Metadata_OfAWaveFile()
    {
        var path = Wave("stereo.wav", 0.5, Sine(0.5), Sine(0.5));

        var metadata = AudioBASS.GetMetadata(path);

        Assert.Equal(2, metadata.Channels);
        Assert.Equal(500, metadata.DurationMs, 1);
        Assert.Equal(SampleRate * 2 * 16 / 1000d, metadata.Bitrate, 1d);
        Assert.Equal(metadata.Format, AudioBASS.GetFormat(path));
        Assert.Equal(metadata.Channels, AudioBASS.GetChannels(path));
        Assert.Equal(metadata.DurationMs, AudioBASS.GetDuration(path));
        Assert.Equal(metadata.Bitrate, AudioBASS.GetBitrate(path));
    }

    [Fact]
    public void FullInfo_IsTheMetadataAndPeaks()
    {
        var path = Wave("mono.wav", 0.1, Sine(0.5));

        var info = AudioBASS.GetFullInfo(path);

        Assert.Equal(1, info.Channels);
        Assert.Equal(100, info.DurationMs, 1);
        Assert.Equal(
            AudioBASS.GetPeaks(path).Select(peak => peak[0]),
            info.Peaks.Select(peak => peak[0])
        );
    }

    [Fact]
    public void Peaks_AreOnePerMsAndChannel()
    {
        // Silent for the first half, then a tone in the left channel only.
        var path = Wave("peaks.wav", 0.2, t => t < 0.1 ? 0 : Sine(0.5)(t), Silence);

        var peaks = AudioBASS.GetPeaks(path);

        Assert.InRange(peaks.Count, 199, 201);
        Assert.All(peaks, peak => Assert.Equal(0, peak[1], 0.001f));
        Assert.All(peaks.Take(99), peak => Assert.Equal(0, peak[0], 0.001f));
        Assert.All(peaks.Skip(101).Take(97), peak => Assert.Equal(0.5f, peak[0], 0.02f));
    }

    [Fact]
    public void BrokenFile_IsReportedByName()
    {
        var path = Path.Combine(folder, "broken.wav");
        File.WriteAllText(path, "not audio");

        Assert.Contains(
            "broken.wav",
            Assert.Throws<BadImageFormatException>(() => AudioBASS.GetMetadata(path)).Message
        );
        Assert.Contains(
            "broken.wav",
            Assert.Throws<BadImageFormatException>(() => AudioAnalyzer.GetLevelInfo(path)).Message
        );
    }

    [Fact]
    public void WithoutSamples_ThereAreNoPeaks()
    {
        var path = Wave("empty.wav", 0, Silence);

        Assert.Empty(AudioBASS.GetPeaks(path));
        Assert.Equal(0, AudioBASS.GetDuration(path));
    }

    [Fact]
    public void AudioInfo_OfAWaveFile()
    {
        var info = AudioAnalyzer.GetAudioInfo(Wave("info.wav", 0.25, Sine(0.5), Silence));

        Assert.Equal(2, info.Channels);
        Assert.Equal(SampleRate, info.SampleRate);
        Assert.Equal(250, info.DurationMs, 1);
        Assert.False(info.IsFloatingPoint);
    }

    [Fact]
    public void Levels_ArePerChannel_AndClippingIsLocated()
    {
        // Full scale only between 200 and 300 ms, and only on the left.
        var path = Wave(
            "levels.wav",
            0.5,
            t => Sine(t is >= 0.2 and < 0.3 ? 1 : 0.25)(t),
            Sine(0.5)
        );

        var levels = AudioAnalyzer.GetLevelInfo(path);

        Assert.Equal(1, levels.PeakLeft, 0.01f);
        Assert.Equal(0.5f, levels.PeakRight, 0.01f);
        Assert.Equal(50, levels.SampleCount);
        Assert.Equal(10, levels.ClippingCount);
        Assert.Equal(200, levels.ClippingTimestamps[0]);
        Assert.Equal(290, levels.ClippingTimestamps[^1]);
    }

    [Fact]
    public void Rms_OfASine_IsItsAmplitudeOverTheRootOfTwo()
    {
        var levels = AudioAnalyzer.GetLevelInfo(Wave("rms.wav", 0.5, Sine(0.5)));

        Assert.Equal(0.5 / Math.Sqrt(2), levels.RmsLeft, 0.01);
    }

    [Fact]
    public void MonoLevels_AreTheSameOnBothSides()
    {
        var path = Wave("mono.wav", 0.2, Sine(0.5));

        var samples = AudioAnalyzer.GetChannelLevelsOverTime(path);

        Assert.Equal([0, 50, 100, 150], samples.Select(sample => sample.TimeMs));
        Assert.All(
            samples,
            sample =>
            {
                Assert.Equal(0.5f, sample.LeftLevel, 0.01f);
                Assert.Equal(sample.LeftLevel, sample.RightLevel);
            }
        );
    }

    [Fact]
    public void Spectrogram_PeaksAtTheTonesFrequency()
    {
        var path = Wave("tone.wav", 0.5, Sine(0.8, 5000));

        var data = AudioAnalyzer.GetSpectrogramData(path, fftSize: 2048, timeResolutionMs: 50);

        Assert.Equal(SampleRate, data.SampleRate);
        Assert.Equal(1024, data.FrequencyBins.Length);
        Assert.Equal((double)SampleRate / 2048, data.FrequencyBins[1], 6);
        Assert.InRange(data.Frames.Count, 9, 11);
        Assert.Equal(100, data.Frames[2].TimeMs, 1);
        Assert.Equal(5000, LoudestFrequency(data.Frames[3].Magnitudes, 2048), 30d);
    }

    [Theory]
    [InlineData(DataFlags.FFT256, 256)]
    [InlineData(DataFlags.FFT512, 512)]
    [InlineData(DataFlags.FFT1024, 1024)]
    [InlineData(DataFlags.FFT2048, 2048)]
    [InlineData(DataFlags.FFT4096, 4096)]
    [InlineData(DataFlags.FFT8192, 8192)]
    [InlineData(DataFlags.FFT16384, 16384)]
    [InlineData(DataFlags.FFT32768, 32768)]
    public void Fft_HasHalfAsManyBinsAsSamples(DataFlags flag, int size)
    {
        var fft = AudioAnalyzer.GetFftData(Wave("fft.wav", 0.5, Sine(0.8, 5000)), flag);

        Assert.Equal(size / 2, fft.Length);
        Assert.Equal(5000, LoudestFrequency(fft, size), (double)SampleRate / size);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(4096)]
    [InlineData(8192)]
    [InlineData(16384)]
    [InlineData(32768)]
    public void Spectrogram_OfEverySize(int size)
    {
        var data = AudioAnalyzer.GetSpectrogramData(
            Wave($"size{size}.wav", 1, Sine(0.8, 5000)),
            fftSize: size,
            timeResolutionMs: 200
        );

        Assert.Equal(size / 2, data.FrequencyBins.Length);
        Assert.Equal(
            5000,
            LoudestFrequency(data.Frames[0].Magnitudes, size),
            (double)SampleRate / size
        );
    }

    [Fact]
    public void Fft_AtAPosition_HearsWhatPlaysThere()
    {
        var path = Wave("change.wav", 1, t => Sine(0.8, t < 0.5 ? 2000 : 8000)(t));

        Assert.Equal(
            2000,
            LoudestFrequency(AudioAnalyzer.GetFftDataAtPosition(path, 100), 4096),
            15d
        );
        Assert.Equal(
            8000,
            LoudestFrequency(AudioAnalyzer.GetFftDataAtPosition(path, 700), 4096),
            15d
        );
    }

    private static double LoudestFrequency(float[] bins, int fftSize) =>
        (double)Array.IndexOf(bins, bins.Max()) * SampleRate / fftSize;

    [Fact]
    public void Formats_AreNamed()
    {
        Assert.Equal("OGG", AudioBASS.EnumToString(ChannelType.OGG));
        // Without a name of its own, by what it's made of.
        Assert.Equal(
            "None|ReadOnly",
            AudioBASS.EnumToString((FileAttributes)(1 << 25) | FileAttributes.ReadOnly)
        );
    }

    /// <summary> One chunk holds 65536 values, after which the rest is read. </summary>
    [Theory]
    [InlineData(65536)]
    [InlineData(65537)]
    public void Peaks_ContinueAfterAFullChunk(int frames)
    {
        var path = Wave("long.wav", (frames + 0.5) / SampleRate, Sine(0.5));

        Assert.InRange(AudioBASS.GetPeaks(path).Count, 1486, 1487);
    }
}
