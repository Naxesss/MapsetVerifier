namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    /// Reads metadata out of an FLV file. Everything of interest lives in the "onMetaData" script
    /// tag, which encoders write as the very first tag.
    /// </summary>
    internal static class FlvMetadataParser
    {
        /// <summary> The metadata tag sits at the start of the file; the rest is media data. </summary>
        private const int MaxHeaderBytes = 1024 * 1024;

        private const byte ScriptDataTag = 18;

        /// <summary> Returns null when the file has no "onMetaData" tag, so the caller can fall back. </summary>
        public static VideoMetadata? Parse(Stream stream)
        {
            var length = (int)Math.Min(stream.Length, MaxHeaderBytes);

            if (length < 13)
                return null;

            var buffer = new byte[length];
            stream.Position = 0;

            try
            {
                stream.ReadExactly(buffer, 0, length);
            }
            catch (EndOfStreamException)
            {
                return null;
            }

            var reader = new MediaByteReader(buffer);

            reader.Skip(4); // Signature and version.

            if (!reader.TryReadByte(out var flags))
                return null;

            if (!reader.TryReadUInt32BigEndian(out var dataOffset) || dataOffset > buffer.Length)
                return null;

            var metadata = new VideoMetadata { Container = "FLV", Source = "flv" };

            metadata.HasAudioTrack = (flags & 0x04) != 0;

            reader.Position = (int)dataOffset;

            var properties = FindMetaDataProperties(reader);

            if (properties == null)
                return null;

            metadata.Width = (int)GetNumber(properties, "width");
            metadata.Height = (int)GetNumber(properties, "height");
            metadata.DurationMs = GetNumber(properties, "duration") * 1000;

            var frameRate = GetNumber(properties, "framerate");

            if (frameRate > 0)
                metadata.FrameRate = frameRate;

            // Both rates are stored in kilobits per second.
            var videoDataRate = GetNumber(properties, "videodatarate");

            if (videoDataRate > 0)
                metadata.VideoBitrateBps = (long)Math.Round(videoDataRate * 1000);

            if (properties.TryGetValue("videocodecid", out var videoCodecId))
                metadata.VideoCodec = MediaCodecNames.ForFlvVideoCodecId((int)videoCodecId);

            if (properties.TryGetValue("audiocodecid", out var audioCodecId))
            {
                metadata.HasAudioTrack = true;
                metadata.AudioCodec = MediaCodecNames.ForFlvAudioCodecId((int)audioCodecId);
            }

            metadata.AudioSampleRate = (int)GetNumber(properties, "audiosamplerate");

            if (GetNumber(properties, "stereo") > 0)
                metadata.AudioChannels = 2;
            else if (metadata.HasAudioTrack)
                metadata.AudioChannels = 1;

            if (metadata.Width == 0 && metadata.Height == 0)
                return null;

            return metadata;
        }

        /// <summary> Walks the tags until the "onMetaData" script tag is found. </summary>
        private static Dictionary<string, double>? FindMetaDataProperties(MediaByteReader reader)
        {
            while (reader.Remaining >= 15)
            {
                reader.Skip(4); // Size of the previous tag.

                if (!reader.TryReadByte(out var tagType))
                    return null;

                if (!TryReadUInt24BigEndian(reader, out var dataSize))
                    return null;

                reader.Skip(3); // Timestamp.
                reader.Skip(1); // Timestamp extension.
                reader.Skip(3); // Stream id.

                var dataStart = reader.Position;

                if (dataSize > reader.Remaining)
                    return null;

                if (tagType == ScriptDataTag)
                {
                    var properties = ReadScriptData(reader);

                    if (properties != null)
                        return properties;
                }

                reader.Position = dataStart + (int)dataSize;
            }

            return null;
        }

        private static Dictionary<string, double>? ReadScriptData(MediaByteReader reader)
        {
            if (!reader.TryReadByte(out var nameType) || nameType != 0x02)
                return null;

            if (!TryReadAmfString(reader, out var name) || name != "onMetaData")
                return null;

            if (!reader.TryReadByte(out var valueType))
                return null;

            // An ECMA array carries a (non-binding) element count before its pairs; an object does not.
            if (valueType == 0x08)
                reader.Skip(4);
            else if (valueType != 0x03)
                return null;

            var properties = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            while (reader.Remaining > 2)
            {
                if (!TryReadAmfString(reader, out var key))
                    return properties;

                if (!reader.TryReadByte(out var type))
                    return properties;

                switch (type)
                {
                    case 0x00: // Number.
                        if (!reader.TryReadDoubleBigEndian(out var number))
                            return properties;

                        if (key.Length > 0)
                            properties[key] = number;

                        break;

                    case 0x01: // Boolean.
                        if (!reader.TryReadByte(out var boolean))
                            return properties;

                        if (key.Length > 0)
                            properties[key] = boolean;

                        break;

                    case 0x02: // String.
                        if (!TryReadAmfString(reader, out _))
                            return properties;

                        break;

                    case 0x09: // End of object.
                        return properties;

                    default:
                        // Nested values are not needed for any of the fields we surface, and their
                        // length cannot be skipped over blindly, so stop here.
                        return properties;
                }
            }

            return properties;
        }

        private static bool TryReadAmfString(MediaByteReader reader, out string value)
        {
            value = string.Empty;

            return reader.TryReadUInt16BigEndian(out var length)
                && reader.TryReadUtf8(length, out value);
        }

        private static bool TryReadUInt24BigEndian(MediaByteReader reader, out uint value)
        {
            value = 0;

            if (
                !reader.TryReadByte(out var high)
                || !reader.TryReadByte(out var middle)
                || !reader.TryReadByte(out var low)
            )
                return false;

            value = (uint)((high << 16) | (middle << 8) | low);

            return true;
        }

        private static double GetNumber(Dictionary<string, double> properties, string key) =>
            properties.TryGetValue(key, out var value) ? value : 0;
    }
}
