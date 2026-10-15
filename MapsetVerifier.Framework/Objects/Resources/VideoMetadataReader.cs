using System.Text;
using Serilog;

namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    /// Reads video metadata from the container headers, without any native media dependency. The
    /// containers osu! accepts are parsed directly; anything else degrades to TagLib, which only
    /// knows resolution, duration and whether an audio track exists.
    /// </summary>
    public static class VideoMetadataReader
    {
        private const string TagLibFallbackWarning =
            "This container is not parsed directly, so frame rate, codec and video bitrate are unavailable.";

        /// <summary>
        /// Never throws for unreadable or corrupt files; the returned metadata is filled in as far
        /// as the file allowed, with the rest explained in <see cref="VideoMetadata.Warnings" />.
        /// </summary>
        public static VideoMetadata Read(string filePath)
        {
            var metadata = ReadContainer(filePath) ?? ReadWithTagLib(filePath);

            if (metadata.Container == "Unknown")
                metadata.Container = GetExtensionName(filePath);

            try
            {
                metadata.FileSizeBytes = new FileInfo(filePath).Length;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Could not read the size of video {Path}", filePath);
            }

            if (metadata.DurationMs > 0 && metadata.FileSizeBytes > 0)
                metadata.OverallBitrateBps = (long)
                    Math.Round(metadata.FileSizeBytes * 8 / (metadata.DurationMs / 1000));

            return metadata;
        }

        /// <summary>
        /// Determines the container from the file's own bytes rather than its extension, which
        /// beatmap sets get wrong often enough to matter. Returns null when it is not one of the
        /// containers parsed directly.
        /// </summary>
        public static string? SniffContainer(string filePath)
        {
            try
            {
                using var stream = OpenRead(filePath);

                return SniffContainer(stream);
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Could not read the signature of video {Path}", filePath);

                return null;
            }
        }

        private static VideoMetadata? ReadContainer(string filePath)
        {
            try
            {
                using var stream = OpenRead(filePath);

                return SniffContainer(stream) switch
                {
                    "MP4" => Mp4MetadataParser.Parse(stream),
                    "AVI" => AviMetadataParser.Parse(stream),
                    "FLV" => FlvMetadataParser.Parse(stream),
                    _ => null,
                };
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Could not parse the container of video {Path}", filePath);

                return null;
            }
        }

        private static string? SniffContainer(Stream stream)
        {
            var signature = new byte[12];

            stream.Position = 0;

            if (stream.Read(signature, 0, signature.Length) < signature.Length)
                return null;

            var leading = Encoding.ASCII.GetString(signature, 0, 4);
            var atOffsetFour = Encoding.ASCII.GetString(signature, 4, 4);
            var atOffsetEight = Encoding.ASCII.GetString(signature, 8, 4);

            if (atOffsetFour is "ftyp" or "moov" or "mdat" or "free" or "skip" or "wide")
                return "MP4";

            if (leading == "RIFF" && atOffsetEight == "AVI ")
                return "AVI";

            if (leading.StartsWith("FLV", StringComparison.Ordinal))
                return "FLV";

            return null;
        }

        private static FileStream OpenRead(string filePath) =>
            new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        private static VideoMetadata ReadWithTagLib(string filePath)
        {
            var metadata = new VideoMetadata
            {
                Source = "taglib",
                Container = GetExtensionName(filePath),
            };

            // Disposing the tag file only rewinds the abstraction's stream, so the file handle has
            // to be released separately.
            var abstraction = new FileAbstraction(filePath);

            try
            {
                using var tagFile = abstraction.GetTagFile();

                if (tagFile?.Properties == null)
                {
                    metadata.Warnings.Add(
                        "This file could not be read as a video, so no details are available."
                    );

                    return metadata;
                }

                metadata.Width = tagFile.Properties.VideoWidth;
                metadata.Height = tagFile.Properties.VideoHeight;
                metadata.DurationMs = tagFile.Properties.Duration.TotalMilliseconds;
                metadata.AudioChannels = tagFile.Properties.AudioChannels;
                metadata.AudioSampleRate = tagFile.Properties.AudioSampleRate;
                metadata.HasAudioTrack = tagFile.Properties.AudioChannels > 0;
                metadata.Warnings.Add(TagLibFallbackWarning);
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Could not read video {Path} with TagLib", filePath);

                metadata.Warnings.Add(
                    "This file could not be read as a video, so no details are available."
                );
            }
            finally
            {
                abstraction.ReadStream.Dispose();
            }

            return metadata;
        }

        private static string GetExtensionName(string filePath)
        {
            var extension = Path.GetExtension(filePath).TrimStart('.');

            return extension.Length > 0 ? extension.ToUpperInvariant() : "Unknown";
        }
    }
}
