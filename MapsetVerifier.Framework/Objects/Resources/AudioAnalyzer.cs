using System.Collections.Concurrent;
using ManagedBass;

namespace MapsetVerifier.Framework.Objects.Resources;

/// <summary>
/// Extended audio analysis functionality for spectral analysis, dynamic range, and advanced metrics.
/// </summary>
public static class AudioAnalyzer
{
    private static readonly ConcurrentDictionary<string, object> Locks = new();

    /// <summary>
    /// Gets comprehensive audio information including sample rate, bit depth, and format details.
    /// </summary>
    public static AudioInfo GetAudioInfo(string filePath)
    {
        lock (Locks.GetOrAdd(filePath, new object()))
        {
            var stream = CreateStream(filePath);
            try
            {
                Bass.ChannelGetInfo(stream, out var channelInfo);
                var length = Bass.ChannelGetLength(stream);
                var seconds = Bass.ChannelBytes2Seconds(stream, length);
                var bitrate = Bass.ChannelGetAttribute(stream, ChannelAttribute.Bitrate);

                return new AudioInfo
                {
                    Channels = channelInfo.Channels,
                    SampleRate = channelInfo.Frequency,
                    ChannelType = channelInfo.ChannelType,
                    DurationMs = seconds * 1000,
                    Bitrate = bitrate,
                    IsFloatingPoint = (channelInfo.Flags & BassFlags.Float) != 0
                };
            }
            finally
            {
                Bass.StreamFree(stream);
            }
        }
    }

    /// <summary>
    /// Performs FFT analysis and returns frequency magnitude data.
    /// </summary>
    public static float[] GetFftData(string filePath, DataFlags fftSize = DataFlags.FFT4096)
    {
        lock (Locks.GetOrAdd(filePath, new object()))
        {
            var stream = CreateStream(filePath);
            try
            {
                var fftLength = GetFftLength(fftSize);
                var fftData = new float[fftLength];
                Bass.ChannelGetData(stream, fftData, (int)fftSize);
                return fftData;
            }
            finally
            {
                Bass.StreamFree(stream);
            }
        }
    }

    /// <summary>
    /// Gets FFT data at a specific time position in the audio.
    /// </summary>
    public static float[] GetFftDataAtPosition(string filePath, double positionMs, DataFlags fftSize = DataFlags.FFT4096)
    {
        lock (Locks.GetOrAdd(filePath, new object()))
        {
            var stream = CreateStream(filePath);
            try
            {
                var positionBytes = Bass.ChannelSeconds2Bytes(stream, positionMs / 1000.0);
                Bass.ChannelSetPosition(stream, positionBytes);

                var fftLength = GetFftLength(fftSize);
                var fftData = new float[fftLength];
                Bass.ChannelGetData(stream, fftData, (int)fftSize);
                return fftData;
            }
            finally
            {
                Bass.StreamFree(stream);
            }
        }
    }

    /// <summary>
    /// Gets peak and RMS levels for the entire audio file.
    /// </summary>
    public static LevelInfo GetLevelInfo(string filePath, int sampleIntervalMs = 10)
    {
        lock (Locks.GetOrAdd(filePath, new object()))
        {
            var stream = CreateStream(filePath);
            try
            {
                Bass.ChannelGetInfo(stream, out var channelInfo);
                var length = Bass.ChannelGetLength(stream);
                var seconds = Bass.ChannelBytes2Seconds(stream, length);
                var totalSamples = (int)(seconds * 1000 / sampleIntervalMs);

                var peakLeft = 0f;
                var peakRight = 0f;
                var sumSquaredLeft = 0.0;
                var sumSquaredRight = 0.0;
                var sampleCount = 0;
                var clippingCount = 0;
                var clippingTimestamps = new List<double>();

                var levels = new float[channelInfo.Channels];
                var intervalSeconds = sampleIntervalMs / 1000f;

                for (var i = 0; i < totalSamples; i++)
                {
                    if (!Bass.ChannelGetLevel(stream, levels, intervalSeconds, 0))
                    {
                        if (Bass.LastError == Errors.Ended) break;
                        continue;
                    }

                    var left = levels[0];
                    var right = channelInfo.Channels > 1 ? levels[1] : levels[0];

                    if (left > peakLeft) peakLeft = left;
                    if (right > peakRight) peakRight = right;

                    sumSquaredLeft += left * left;
                    sumSquaredRight += right * right;
                    sampleCount++;

                    // Detect clipping (level >= 0.99)
                    if (left >= 0.99f || right >= 0.99f)
                    {
                        clippingCount++;
                        clippingTimestamps.Add(i * sampleIntervalMs);
                    }
                }

                var rmsLeft = sampleCount > 0 ? Math.Sqrt(sumSquaredLeft / sampleCount) : 0;
                var rmsRight = sampleCount > 0 ? Math.Sqrt(sumSquaredRight / sampleCount) : 0;

                return new LevelInfo
                {
                    PeakLeft = peakLeft,
                    PeakRight = peakRight,
                    RmsLeft = rmsLeft,
                    RmsRight = rmsRight,
                    ClippingCount = clippingCount,
                    ClippingTimestamps = clippingTimestamps,
                    SampleCount = sampleCount
                };
            }
            finally
            {
                Bass.StreamFree(stream);
            }
        }
    }

    /// <summary>
    /// Gets channel balance data over time for visualization.
    /// </summary>
    public static List<ChannelLevelSample> GetChannelLevelsOverTime(string filePath, int sampleIntervalMs = 50)
    {
        lock (Locks.GetOrAdd(filePath, new object()))
        {
            var stream = CreateStream(filePath);
            try
            {
                Bass.ChannelGetInfo(stream, out var channelInfo);
                var length = Bass.ChannelGetLength(stream);
                var seconds = Bass.ChannelBytes2Seconds(stream, length);
                var totalSamples = (int)(seconds * 1000 / sampleIntervalMs);

                var samples = new List<ChannelLevelSample>();
                var levels = new float[channelInfo.Channels];
                var intervalSeconds = sampleIntervalMs / 1000f;

                for (var i = 0; i < totalSamples; i++)
                {
                    if (!Bass.ChannelGetLevel(stream, levels, intervalSeconds, 0))
                    {
                        if (Bass.LastError == Errors.Ended) break;
                        continue;
                    }

                    samples.Add(new ChannelLevelSample
                    {
                        TimeMs = i * sampleIntervalMs,
                        LeftLevel = levels[0],
                        RightLevel = channelInfo.Channels > 1 ? levels[1] : levels[0]
                    });
                }

                return samples;
            }
            finally
            {
                Bass.StreamFree(stream);
            }
        }
    }

    /// <summary>
    /// Gets spectrogram data for visualization.
    /// </summary>
    public static SpectrogramData GetSpectrogramData(string filePath, int fftSize = 4096, int timeResolutionMs = 10)
    {
        lock (Locks.GetOrAdd(filePath, new object()))
        {
            var stream = CreateStream(filePath);
            try
            {
                Bass.ChannelGetInfo(stream, out var channelInfo);
                var length = Bass.ChannelGetLength(stream);
                var seconds = Bass.ChannelBytes2Seconds(stream, length);
                var totalFrames = (int)(seconds * 1000 / timeResolutionMs);

                var fftFlag = GetFftFlag(fftSize);
                var fftLength = fftSize / 2;
                var frames = new List<SpectrogramFrame>();
                var fftData = new float[fftLength];
                var intervalSeconds = timeResolutionMs / 1000f;

                // Calculate frequency bins
                var frequencyBins = new double[fftLength];
                var binWidth = (double)channelInfo.Frequency / fftSize;
                for (var i = 0; i < fftLength; i++)
                {
                    frequencyBins[i] = i * binWidth;
                }

                for (var i = 0; i < totalFrames; i++)
                {
                    var bytesRead = Bass.ChannelGetData(stream, fftData, (int)fftFlag);
                    if (bytesRead <= 0)
                    {
                        if (Bass.LastError == Errors.Ended) break;
                        continue;
                    }

                    // Convert to dB scale
                    var magnitudes = new float[fftLength];
                    for (var j = 0; j < fftLength; j++)
                    {
                        var magnitude = fftData[j];
                        magnitudes[j] = magnitude > 0 ? (float)(20 * Math.Log10(magnitude)) : -100f;
                    }

                    frames.Add(new SpectrogramFrame
                    {
                        TimeMs = i * timeResolutionMs,
                        Magnitudes = magnitudes
                    });
                }

                return new SpectrogramData
                {
                    Frames = frames,
                    FrequencyBins = frequencyBins,
                    SampleRate = channelInfo.Frequency,
                    FftSize = fftSize
                };
            }
            finally
            {
                Bass.StreamFree(stream);
            }
        }
    }

    private static int CreateStream(string filePath)
    {
        AudioBASS.EnsureInitialized();
        var stream = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Float);
        if (stream == 0)
            throw new BadImageFormatException($"Could not create stream of \"{Path.GetFileName(filePath)}\", error \"{Bass.LastError}\".");
        return stream;
    }

    private static int GetFftLength(DataFlags fftSize)
    {
        return fftSize switch
        {
            DataFlags.FFT256 => 128,
            DataFlags.FFT512 => 256,
            DataFlags.FFT1024 => 512,
            DataFlags.FFT2048 => 1024,
            DataFlags.FFT4096 => 2048,
            DataFlags.FFT8192 => 4096,
            DataFlags.FFT16384 => 8192,
            DataFlags.FFT32768 => 16384,
            _ => 2048
        };
    }

    private static DataFlags GetFftFlag(int fftSize)
    {
        return fftSize switch
        {
            256 => DataFlags.FFT256,
            512 => DataFlags.FFT512,
            1024 => DataFlags.FFT1024,
            2048 => DataFlags.FFT2048,
            4096 => DataFlags.FFT4096,
            8192 => DataFlags.FFT8192,
            16384 => DataFlags.FFT16384,
            32768 => DataFlags.FFT32768,
            _ => DataFlags.FFT4096
        };
    }
}

/// <summary>
/// Basic audio file information.
/// </summary>
public struct AudioInfo
{
    public int Channels { get; init; }
    public int SampleRate { get; init; }
    public ChannelType ChannelType { get; init; }
    public double DurationMs { get; init; }
    public double Bitrate { get; init; }
    public bool IsFloatingPoint { get; init; }
}

/// <summary>
/// Audio level information including peaks and RMS.
/// </summary>
public struct LevelInfo
{
    public float PeakLeft { get; init; }
    public float PeakRight { get; init; }
    public double RmsLeft { get; init; }
    public double RmsRight { get; init; }
    public int ClippingCount { get; init; }
    public List<double> ClippingTimestamps { get; init; }
    public int SampleCount { get; init; }
}

/// <summary>
/// A single channel level sample at a point in time.
/// </summary>
public struct ChannelLevelSample
{
    public double TimeMs { get; init; }
    public float LeftLevel { get; init; }
    public float RightLevel { get; init; }
}

/// <summary>
/// A single frame of spectrogram data.
/// </summary>
public struct SpectrogramFrame
{
    public double TimeMs { get; init; }
    public float[] Magnitudes { get; init; }
}

/// <summary>
/// Complete spectrogram data for visualization.
/// </summary>
public struct SpectrogramData
{
    public List<SpectrogramFrame> Frames { get; init; }
    public double[] FrequencyBins { get; init; }
    public int SampleRate { get; init; }
    public int FftSize { get; init; }
}

