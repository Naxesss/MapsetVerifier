using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ManagedBass;

namespace MapsetVerifier.Framework.Objects.Resources
{
    public static class AudioBASS
    {
        private static readonly ConcurrentDictionary<string, object> locks = new();
        private static bool isInitialized;

        private static void Initialize()
        {
            if (!isInitialized)
            {
                // 0 = No Output Device
                if (!Bass.Init(0) && Bass.LastError != Errors.Already)
                    throw new BadImageFormatException($"Could not initialize ManagedBass, error \"{Bass.LastError}\".");

                isInitialized = true;
            }
        }

        private static int CreateStream(string filePath)
        {
            Initialize();

            var stream = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode);

            if (stream == 0)
                throw new BadImageFormatException($"Could not create stream of \"{filePath.Split('\\', '/').Last()}\", error \"{Bass.LastError}\".");

            return stream;
        }

        private static void FreeStream(int stream) => Bass.StreamFree(stream);

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

        /// <summary> Returns the audio duration in ms, given the full path. </summary>
        public static double GetDuration(string filePath)
        {
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath);
                var length = Bass.ChannelGetLength(stream);
                var seconds = Bass.ChannelBytes2Seconds(stream, length);

                FreeStream(stream);

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
        public static List<float[]> GetPeaks(string filePath)
        {
            lock (locks.GetOrAdd(filePath, new object()))
            {
                var stream = CreateStream(filePath);
                var length = Bass.ChannelGetLength(stream);
                var seconds = Bass.ChannelBytes2Seconds(stream, length);

                Bass.ChannelGetInfo(stream, out var channelInfo);

                var peaks = new List<float[]>();

                for (var i = 0; i < (int)(seconds * 1000); ++i)
                {
                    var levels = new float[channelInfo.Channels];

                    var success = Bass.ChannelGetLevel(stream, levels, 0.001f, 0);

                    if (!success)
                    {
                        var error = Bass.LastError;

                        if (error != Errors.Ended)
                            throw new BadImageFormatException($"Could not parse audio peak of \"{filePath.Split('\\', '/').Last()}\" at " + i * 1000 + " ms.");

                        break;
                    }

                    peaks.Add(levels);
                }

                FreeStream(stream);

                return peaks;
            }
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