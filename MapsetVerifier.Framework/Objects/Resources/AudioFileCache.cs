using System.Collections.Concurrent;
using ManagedBass;

namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    ///     Caches the results of <see cref="AudioBASS" /> calls per file path, so that multiple checks
    ///     inspecting the same audio file (e.g. the various hit sound checks) don't each decode it from
    ///     scratch. Available to custom check plugins as well, since it lives in the Framework assembly.
    /// </summary>
    public static class AudioFileCache
    {
        // Format/channels/duration/bitrate are all read from one stream open (AudioBASS.GetMetadata),
        // so they share a single cache entry per file instead of one each.
        private static readonly ConcurrentDictionary<
            string,
            Lazy<AudioBASS.AudioMetadata>
        > metadataCache = new();

        // Peaks require a full ms-by-ms decode and aren't needed by every caller of the metadata
        // above (e.g. format/bitrate checks on the main song file never need peaks), so they're kept
        // as a separate, independently-triggered cache entry.
        private static readonly ConcurrentDictionary<string, Lazy<List<float[]>>> peaksCache =
            new();

        private static AudioBASS.AudioMetadata GetMetadata(string filePath) =>
            metadataCache
                .GetOrAdd(
                    filePath,
                    path => new Lazy<AudioBASS.AudioMetadata>(() => AudioBASS.GetMetadata(path))
                )
                .Value;

        /// <summary> Cached version of <see cref="AudioBASS.GetFormat" />. </summary>
        public static ChannelType GetFormat(string filePath) => GetMetadata(filePath).Format;

        /// <summary> Cached version of <see cref="AudioBASS.GetChannels" />. </summary>
        public static int GetChannels(string filePath) => GetMetadata(filePath).Channels;

        /// <summary> Cached version of <see cref="AudioBASS.GetDuration" />. </summary>
        public static double GetDuration(string filePath) => GetMetadata(filePath).DurationMs;

        /// <summary> Cached version of <see cref="AudioBASS.GetBitrate" />. </summary>
        public static double GetBitrate(string filePath) => GetMetadata(filePath).Bitrate;

        /// <summary> Cached version of <see cref="AudioBASS.GetPeaks" />. </summary>
        public static List<float[]> GetPeaks(string filePath) =>
            peaksCache
                .GetOrAdd(filePath, path => new Lazy<List<float[]>>(() => AudioBASS.GetPeaks(path)))
                .Value;

        /// <summary>
        ///     Clears all cached values. Should be called between beatmap set check runs so the cache
        ///     doesn't grow unbounded across a long-running session, e.g. in the GUI or server.
        /// </summary>
        public static void Clear()
        {
            metadataCache.Clear();
            peaksCache.Clear();
        }
    }
}
