namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    /// Reads stream metadata out of a RIFF/AVI file. Only the header list is read, which sits at the
    /// very start of the file, so the size of the file itself does not matter.
    /// </summary>
    internal static class AviMetadataParser
    {
        /// <summary> The header list is tiny; anything larger than this is not a header list. </summary>
        private const int MaxHeaderBytes = 1024 * 1024;

        /// <summary> Returns null when the file has no usable header list, so the caller can fall back. </summary>
        public static VideoMetadata? Parse(Stream stream)
        {
            var header = ReadHeaderList(stream);

            if (header == null)
                return null;

            var reader = new MediaByteReader(header);
            var metadata = new VideoMetadata { Container = "AVI", Source = "avi" };

            double microsecondsPerFrame = 0;
            uint totalFrames = 0;
            var streamType = string.Empty;
            double streamFrameRate = 0;
            uint streamLength = 0;

            while (reader.Remaining >= 8)
            {
                if (!reader.TryReadFourCc(out var chunkType))
                    break;

                if (!reader.TryReadUInt32LittleEndian(out var chunkSize))
                    break;

                if (chunkSize > reader.Remaining)
                    break;

                var chunkStart = reader.Position;

                switch (chunkType)
                {
                    case "LIST":
                        reader.TryReadFourCc(out var listType);

                        // Descend into the header lists, since the stream headers we need are nested
                        // inside them. Everything else, "movi" in particular, is media data.
                        if (listType is "hdrl" or "strl" or "odml")
                            continue;

                        break;

                    case "avih":
                        if (reader.TryReadUInt32LittleEndian(out var microseconds))
                            microsecondsPerFrame = microseconds;

                        reader.Skip(4); // Max bytes per second.
                        reader.Skip(4); // Padding granularity.
                        reader.Skip(4); // Flags.
                        reader.TryReadUInt32LittleEndian(out totalFrames);
                        reader.Skip(4); // Initial frames.
                        reader.Skip(4); // Stream count.
                        reader.Skip(4); // Suggested buffer size.

                        if (reader.TryReadUInt32LittleEndian(out var width))
                            metadata.Width = (int)width;

                        if (reader.TryReadUInt32LittleEndian(out var height))
                            metadata.Height = (int)height;

                        break;

                    case "strh":
                        reader.TryReadFourCc(out streamType);
                        reader.Skip(4); // Handler; the "strf" chunk carries the more reliable fourcc.
                        reader.Skip(4); // Flags.
                        reader.Skip(2); // Priority.
                        reader.Skip(2); // Language.
                        reader.Skip(4); // Initial frames.
                        reader.TryReadUInt32LittleEndian(out var scale);
                        reader.TryReadUInt32LittleEndian(out var rate);
                        reader.Skip(4); // Start.
                        reader.TryReadUInt32LittleEndian(out streamLength);

                        streamFrameRate = scale > 0 ? (double)rate / scale : 0;

                        break;

                    case "strf" when streamType == "vids":
                        ReadVideoFormat(reader, metadata);

                        if (streamFrameRate > 0)
                        {
                            metadata.FrameRate = streamFrameRate;

                            if (streamLength > 0)
                                metadata.DurationMs = streamLength / streamFrameRate * 1000;
                        }

                        break;

                    case "strf" when streamType == "auds":
                        ReadAudioFormat(reader, metadata);

                        break;
                }

                // Chunks are padded to an even number of bytes.
                reader.Position = chunkStart + (int)chunkSize + (int)(chunkSize % 2);
            }

            if (metadata.Width == 0 && metadata.Height == 0)
                return null;

            if (metadata.FrameRate is null or 0 && microsecondsPerFrame > 0)
                metadata.FrameRate = 1_000_000 / microsecondsPerFrame;

            if (metadata.DurationMs <= 0 && totalFrames > 0 && metadata.FrameRate > 0)
                metadata.DurationMs = totalFrames / metadata.FrameRate.Value * 1000;

            metadata.Warnings.Add(
                "AVI headers do not carry a per-track bitrate, so only the overall bitrate is shown."
            );

            return metadata;
        }

        private static void ReadVideoFormat(MediaByteReader reader, VideoMetadata metadata)
        {
            reader.Skip(4); // Structure size.

            if (reader.TryReadUInt32LittleEndian(out var width) && width > 0)
                metadata.Width = (int)width;

            if (reader.TryReadUInt32LittleEndian(out var height) && height > 0)
                metadata.Height = (int)height;

            reader.Skip(2); // Planes.
            reader.Skip(2); // Bit count.

            if (!reader.TryReadFourCc(out var compression))
                return;

            metadata.VideoCodec = compression.Trim('\0', ' ') is { Length: > 0 } fourCc
                ? MediaCodecNames.ForVideoFourCc(fourCc)
                : "Uncompressed";
        }

        private static void ReadAudioFormat(MediaByteReader reader, VideoMetadata metadata)
        {
            if (!reader.TryReadUInt16LittleEndian(out var formatTag))
                return;

            metadata.HasAudioTrack = true;
            metadata.AudioCodec = MediaCodecNames.ForWaveFormatTag(formatTag);

            if (reader.TryReadUInt16LittleEndian(out var channels))
                metadata.AudioChannels = channels;

            if (reader.TryReadUInt32LittleEndian(out var sampleRate))
                metadata.AudioSampleRate = (int)sampleRate;
        }

        /// <summary>
        /// Reads the bytes following the "RIFF....AVI " signature, capped since the header list sits
        /// at the start of the file and the media data after it is of no interest.
        /// </summary>
        private static byte[]? ReadHeaderList(Stream stream)
        {
            var signature = new byte[12];
            stream.Position = 0;

            try
            {
                stream.ReadExactly(signature, 0, signature.Length);
            }
            catch (EndOfStreamException)
            {
                return null;
            }

            var length = (int)Math.Min(stream.Length - 12, MaxHeaderBytes);

            if (length <= 0)
                return null;

            var header = new byte[length];
            stream.ReadExactly(header, 0, length);

            return header;
        }
    }
}
