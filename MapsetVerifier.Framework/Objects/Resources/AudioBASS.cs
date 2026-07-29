using System.Collections.Concurrent;
using ManagedBass;

namespace MapsetVerifier.Framework.Objects.Resources
{
    public static class AudioBASS
    {
        private static readonly ConcurrentDictionary<string, object> locks = new();
        private static readonly object initLock = new();
        private static volatile bool isInitialized;

        private static void Initialize()
        {
            // isInitialized is only ever set to true here, under initLock, so the outer check is a
            // safe fast-path once initialized. Without this lock, concurrent first-time calls to
            // CreateStream (which happens under a per-FILE lock, not a shared one, since different
            // checks decode different files in parallel) would all race into Bass.Init at once.
            if (isInitialized)
                return;

            lock (initLock)
            {
                if (isInitialized)
                    return;

                // 0 = No Output Device
                if (!Bass.Init(0) && Bass.LastError != Errors.Already)
                    throw new BadImageFormatException(
                        $"Could not initialize ManagedBass, error \"{Bass.LastError}\"."
                    );

                isInitialized = true;
            }
        }

        /// <summary>
        /// Ensures BASS is initialized. Can be called from other classes that need to use BASS directly.
        /// </summary>
        public static void EnsureInitialized() => Initialize();

        private static int CreateStream(string filePath) =>
            CreateStream(filePath, BassFlags.Decode);

        private static int CreateStream(string filePath, BassFlags flags)
        {
            Initialize();

            var stream = Bass.CreateStream(filePath, 0, 0, flags);

            if (stream == 0)
                throw new BadImageFormatException(
                    $"Could not create stream of \"{filePath.Split('\\', '/').Last()}\", error \"{Bass.LastError}\"."
                );

            return stream;
        }

        private static void FreeStream(int stream) => Bass.StreamFree(stream);

        /// <summary>
        ///     Bundles the format, channel count, duration and bitrate of an audio file, all of which
        ///     can be read from a single stream open, given the full path.
        /// </summary>
        public readonly record struct AudioMetadata(
            ChannelType Format,
            int Channels,
            double DurationMs,
            double Bitrate
        );

        /// <summary>
        ///     Bundles everything <see cref="AudioMetadata" /> has plus the peaks, all read from a
        ///     single stream open, given the full path.
        /// </summary>
        public readonly record struct AudioFileInfo(
            ChannelType Format,
            int Channels,
            double DurationMs,
            double Bitrate,
            List<float[]> Peaks
        );

        /// <summary>
        ///     Returns the format, channel count, duration and bitrate of the audio file in a single
        ///     stream open, given the full path. Prefer <see cref="GetFullInfo" /> instead when peaks
        ///     are also needed for the same file, since that reuses the same stream open for both.
        /// </summary>
        public static AudioMetadata GetMetadata(string filePath)
        {
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath);
                var metadata = ReadMetadata(stream, out _);

                FreeStream(stream);

                return metadata;
            }
        }

        /// <summary>
        ///     Returns the format, channel count, duration, bitrate and peaks of the audio file from a
        ///     single stream open, given the full path. Prefer this over the individual getters when more
        ///     than one property is needed for the same file, since opening/decoding a stream is the
        ///     dominant cost, not reading any one property off it.
        /// </summary>
        public static AudioFileInfo GetFullInfo(string filePath)
        {
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath, BassFlags.Decode | BassFlags.Float);
                var metadata = ReadMetadata(stream, out var frequency);

                var samples = ReadAllSamples(stream, filePath);

                FreeStream(stream);

                var peaks = ComputePeaks(samples, Math.Max(metadata.Channels, 1), frequency);

                return new AudioFileInfo(
                    metadata.Format,
                    metadata.Channels,
                    metadata.DurationMs,
                    metadata.Bitrate,
                    peaks
                );
            }
        }

        /// <summary> Reads format, channel count, duration and bitrate off an already-open stream. </summary>
        private static AudioMetadata ReadMetadata(int stream, out int frequency)
        {
            Bass.ChannelGetInfo(stream, out var channelInfo);
            frequency = channelInfo.Frequency;

            var bitrate = Bass.ChannelGetAttribute(stream, ChannelAttribute.Bitrate);

            var length = Bass.ChannelGetLength(stream);
            var durationMs = 0d;

            // ChannelGetLength/ChannelBytes2Seconds return -1 on error (e.g. empty files).
            if (length >= 0)
            {
                var seconds = Bass.ChannelBytes2Seconds(stream, length);

                if (seconds >= 0)
                    durationMs = seconds * 1000;
            }

            return new AudioMetadata(
                channelInfo.ChannelType,
                channelInfo.Channels,
                durationMs,
                bitrate
            );
        }

        /// <summary> Returns the format of the audio file (e.g. mp3, wav, etc), given the full path. </summary>
        public static ChannelType GetFormat(string filePath)
        {
            // Implements a queue to prevent race conditions since Bass is a static library.
            // Also prevents deadlocks through using new object() rather than the file name itself.
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath);
                Bass.ChannelGetInfo(stream, out var channelInfo);

                FreeStream(stream);

                return channelInfo.ChannelType;
            }
        }

        /// <summary> Returns the channel amount (1 = mono, 2 = stereo, etc), given the full path. </summary>
        public static int GetChannels(string filePath)
        {
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath);
                Bass.ChannelGetInfo(stream, out var channelInfo);

                FreeStream(stream);

                return channelInfo.Channels;
            }
        }

        /// <summary> Returns the audio duration in ms, given the full path. Returns 0 for empty/invalid files. </summary>
        public static double GetDuration(string filePath)
        {
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath);
                var length = Bass.ChannelGetLength(stream);

                // ChannelGetLength returns -1 on error (e.g., empty files)
                if (length < 0)
                {
                    FreeStream(stream);
                    return 0;
                }

                var seconds = Bass.ChannelBytes2Seconds(stream, length);

                FreeStream(stream);

                // ChannelBytes2Seconds returns -1 on error
                if (seconds < 0)
                    return 0;

                return seconds * 1000;
            }
        }

        /// <summary>
        ///     Returns the average audio bitrate in kbps, given the full path.
        ///     Seems to have an error margin of about ~0.1 kbps.
        /// </summary>
        public static double GetBitrate(string filePath)
        {
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath);
                var bitrate = Bass.ChannelGetAttribute(stream, ChannelAttribute.Bitrate);

                FreeStream(stream);

                return bitrate;
            }
        }

        /// <summary>
        ///     Returns the normalized audio peaks (split by channel) for each ms (List = time, array = channel),
        ///     given the full path.
        /// </summary>
        /// <remarks>
        ///     Decodes the whole file into memory with a handful of bulk <see cref="Bass.ChannelGetData(int, float[], int)" />
        ///     calls, then computes the per-ms peak (max absolute sample value per channel, matching the default/"All"
        ///     <see cref="LevelRetrievalFlags" /> behaviour of <see cref="Bass.ChannelGetLevel(int, float[], float, LevelRetrievalFlags)" />)
        ///     in managed code. Doing this instead of calling <c>ChannelGetLevel</c> once per millisecond avoids one
        ///     native interop call per ms of audio, which otherwise dominates runtime for files with any real duration.
        /// </remarks>
        public static List<float[]> GetPeaks(string filePath)
        {
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath, BassFlags.Decode | BassFlags.Float);

                Bass.ChannelGetInfo(stream, out var channelInfo);
                var channels = Math.Max(channelInfo.Channels, 1);
                var frequency = channelInfo.Frequency;

                var samples = ReadAllSamples(stream, filePath);

                FreeStream(stream);

                return ComputePeaks(samples, channels, frequency);
            }
        }

        /// <summary> Computes the per-ms peak (max absolute sample value per channel) from a flat, interleaved sample buffer. </summary>
        private static List<float[]> ComputePeaks(List<float> samples, int channels, int frequency)
        {
            if (samples.Count == 0 || frequency <= 0)
                return [];

            var frameCount = samples.Count / channels;
            var samplesPerMs = frequency / 1000.0;
            var msCount = (int)(frameCount / samplesPerMs);

            var peaks = new List<float[]>(msCount);

            var frame = 0;

            for (var ms = 0; ms < msCount; ++ms)
            {
                var frameEnd = Math.Min((int)((ms + 1) * samplesPerMs), frameCount);
                var levels = new float[channels];

                for (; frame < frameEnd; ++frame)
                {
                    var baseIndex = frame * channels;

                    for (var channel = 0; channel < channels; ++channel)
                    {
                        var abs = Math.Abs(samples[baseIndex + channel]);

                        if (abs > levels[channel])
                            levels[channel] = abs;
                    }
                }

                peaks.Add(levels);
            }

            return peaks;
        }

        /// <summary> Bulk-reads all remaining decoded float samples (interleaved by channel) from a decode stream. </summary>
        private static List<float> ReadAllSamples(int stream, string filePath)
        {
            const int chunkFloats = 65536;

            var chunk = new float[chunkFloats];
            var samples = new List<float>();

            while (true)
            {
                var bytesRead = Bass.ChannelGetData(stream, chunk, chunkFloats * sizeof(float));

                if (bytesRead < 0)
                {
                    if (Bass.LastError != Errors.Ended)
                        throw new BadImageFormatException(
                            $"Could not parse audio peaks of \"{filePath.Split('\\', '/').Last()}\", error \"{Bass.LastError}\"."
                        );

                    break;
                }

                if (bytesRead == 0)
                    break;

                var floatsRead = bytesRead / sizeof(float);

                if (floatsRead == chunkFloats)
                    samples.AddRange(chunk);
                else
                    samples.AddRange(chunk[..floatsRead]);

                if (floatsRead < chunkFloats)
                    break;
            }

            return samples;
        }

        // These two methods are mostly for converting GetFormat into a readable format.
        public static IEnumerable<Enum> GetFlags(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public static string EnumToString(Enum input)
        {
            var formatsCorrectly = false;

            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                long.Parse(input.ToString());
            }
            catch
            {
                formatsCorrectly = true;
            }

            return formatsCorrectly ? input.ToString() : string.Join("|", GetFlags(input));
        }
    }
}
