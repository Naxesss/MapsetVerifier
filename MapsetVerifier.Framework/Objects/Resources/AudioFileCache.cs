using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ManagedBass;

namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    ///     Caches the results of <see cref="AudioBASS" /> calls per file content, so that multiple
    ///     checks inspecting the same audio (e.g. the various hit sound checks) don't each decode it
    ///     from scratch - and so byte-identical files reused under different names (common in
    ///     mapsets that duplicate a hit sound across many sample-index filenames) are only decoded
    ///     once. Available to custom check plugins as well, since it lives in the Framework assembly.
    /// </summary>
    public static class AudioFileCache
    {
        // Keyed by content hash rather than file path, so byte-identical files sharing the same
        // audio under different names only get decoded once. Hashing is I/O + a cheap digest, far
        // less than a BASS decode, so it's worth paying even for files that turn out to be unique.
        private static readonly ConcurrentDictionary<string, Lazy<string>> hashCache = new();

        // Format/channels/duration/bitrate are read from one cheap stream open (no sample
        // decoding) and cached separately from peaks, which require decoding the entire file and
        // are considerably more expensive for real (especially compressed) audio. Checks that only
        // need metadata - e.g. the format/bitrate checks on the main song file, which can be
        // several minutes long - must not pay for a full peaks decode they never asked for.
        private static readonly ConcurrentDictionary<
            string,
            Lazy<AudioBASS.AudioMetadata>
        > metadataCache = new();

        private static readonly ConcurrentDictionary<string, Lazy<List<float[]>>> peaksCache =
            new();

        private static string GetContentHash(string filePath) =>
            hashCache
                .GetOrAdd(
                    filePath,
                    path => new Lazy<string>(() =>
                    {
                        using var sha = SHA256.Create();
                        using var stream = File.OpenRead(path);

                        return Convert.ToHexString(sha.ComputeHash(stream));
                    })
                )
                .Value;

        private static AudioBASS.AudioMetadata GetMetadata(string filePath)
        {
            var hash = GetContentHash(filePath);

            return metadataCache
                .GetOrAdd(
                    hash,
                    _ => new Lazy<AudioBASS.AudioMetadata>(() => AudioBASS.GetMetadata(filePath))
                )
                .Value;
        }

        /// <summary> Cached version of <see cref="AudioBASS.GetFormat" />. </summary>
        public static ChannelType GetFormat(string filePath) => GetMetadata(filePath).Format;

        /// <summary> Cached version of <see cref="AudioBASS.GetChannels" />. </summary>
        public static int GetChannels(string filePath) => GetMetadata(filePath).Channels;

        /// <summary> Cached version of <see cref="AudioBASS.GetDuration" />. </summary>
        public static double GetDuration(string filePath) => GetMetadata(filePath).DurationMs;

        /// <summary> Cached version of <see cref="AudioBASS.GetBitrate" />. </summary>
        public static double GetBitrate(string filePath) => GetMetadata(filePath).Bitrate;

        /// <summary> Cached version of <see cref="AudioBASS.GetPeaks" />. </summary>
        public static List<float[]> GetPeaks(string filePath)
        {
            var hash = GetContentHash(filePath);

            return peaksCache
                .GetOrAdd(hash, _ => new Lazy<List<float[]>>(() => AudioBASS.GetPeaks(filePath)))
                .Value;
        }

        /// <summary>
        ///     Decodes metadata and peaks for the given hit sound files up front, using full CPU
        ///     parallelism across files rather than relying on however many checks happen to be
        ///     touching audio concurrently. Decoding real (especially compressed) audio is genuine,
        ///     unavoidable CPU work - one file per check thread only gets a handful of files decoding
        ///     at once, whereas warming with <see cref="Parallel.ForEach{TSource}(IEnumerable{TSource}, Action{TSource})" />
        ///     spreads the whole file set across all available cores before any check needs them.
        /// </summary>
        /// <param name="hitSoundFilePaths"> Full paths to every hit sound file to warm. </param>
        /// <param name="onFileWarmed">
        ///     Invoked once per path after it's been decoded (or found already cached), from
        ///     whichever thread processed it. Optional - for progress reporting only.
        /// </param>
        public static void WarmUp(
            IEnumerable<string> hitSoundFilePaths,
            Action? onFileWarmed = null
        ) =>
            Parallel.ForEach(
                hitSoundFilePaths.Distinct(),
                path =>
                {
                    GetFormat(path);
                    GetPeaks(path);
                    onFileWarmed?.Invoke();
                }
            );

        /// <summary>
        ///     Clears all cached values. Should be called between beatmap set check runs so the cache
        ///     doesn't grow unbounded across a long-running session, e.g. in the GUI or server.
        /// </summary>
        public static void Clear()
        {
            hashCache.Clear();
            metadataCache.Clear();
            peaksCache.Clear();
        }
    }
}
