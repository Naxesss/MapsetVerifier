using System.Buffers.Binary;
using System.Text;

namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    /// Reads track metadata out of an ISO base media file (.mp4, .m4v, .mov) by walking its box
    /// tree. Only the header boxes are read; sample data is never touched.
    /// </summary>
    internal static class Mp4MetadataParser
    {
        /// <summary> Guards against pathological files claiming an absurd number of boxes. </summary>
        private const int MaxBoxesPerLevel = 4096;

        /// <summary> Sample tables above this are not worth reading for a beatmap video. </summary>
        private const int MaxPayloadBytes = 32 * 1024 * 1024;

        /// <summary> Returns null when the file has no usable movie header, so the caller can fall back. </summary>
        public static VideoMetadata? Parse(Stream stream)
        {
            var length = stream.Length;
            var moov = FindBox(ReadBoxes(stream, 0, length), "moov");

            if (moov == null)
                return null;

            var metadata = new VideoMetadata { Container = "MP4", Source = "mp4" };
            var moovBoxes = ReadBoxes(stream, moov.Value.PayloadStart, moov.Value.End);

            var movieDurationSeconds = ReadMovieDuration(stream, moovBoxes);
            var isFragmented = FindBox(moovBoxes, "mvex") != null;

            TrackInfo? videoTrack = null;
            TrackInfo? audioTrack = null;

            foreach (var trak in moovBoxes.Where(box => box.Type == "trak"))
            {
                var track = ReadTrack(stream, trak);

                if (track.Handler == "vide" && videoTrack == null)
                    videoTrack = track;
                else if (track.Handler == "soun" && audioTrack == null)
                    audioTrack = track;
            }

            if (videoTrack == null)
                return null;

            metadata.Width = videoTrack.Width;
            metadata.Height = videoTrack.Height;
            metadata.VideoCodec = MediaCodecNames.ForVideoFourCc(videoTrack.CodecFourCc);
            metadata.VideoCodecProfile = videoTrack.CodecProfile;

            var durationSeconds =
                movieDurationSeconds > 0 ? movieDurationSeconds : videoTrack.DurationSeconds;

            metadata.DurationMs = durationSeconds * 1000;

            if (videoTrack.SampleCount > 0 && videoTrack.SampleDurationSeconds > 0)
            {
                metadata.FrameRate = videoTrack.SampleCount / videoTrack.SampleDurationSeconds;
                metadata.IsVariableFrameRate = videoTrack.VariableFrameRate;

                if (videoTrack.TotalSampleBytes > 0)
                    metadata.VideoBitrateBps = (long)
                        Math.Round(
                            videoTrack.TotalSampleBytes * 8 / videoTrack.SampleDurationSeconds
                        );
            }
            else if (isFragmented)
            {
                metadata.Warnings.Add(
                    "This is a fragmented MP4, so frame rate and video bitrate could not be determined from its headers."
                );
            }

            if (audioTrack != null)
            {
                metadata.HasAudioTrack = true;
                metadata.AudioCodec = MediaCodecNames.ForAudioFourCc(audioTrack.CodecFourCc);
                metadata.AudioChannels = audioTrack.Channels;
                metadata.AudioSampleRate = audioTrack.SampleRate;
            }

            return metadata;
        }

        private static double ReadMovieDuration(Stream stream, List<Box> moovBoxes)
        {
            var mvhd = FindBox(moovBoxes, "mvhd");

            if (mvhd == null)
                return 0;

            var payload = ReadPayload(stream, mvhd.Value);

            if (payload == null)
                return 0;

            var reader = new MediaByteReader(payload);

            if (!reader.TryReadByte(out var version))
                return 0;

            reader.Skip(3); // Flags.

            uint timescale;
            double duration;

            if (version == 1)
            {
                reader.Skip(16); // Creation and modification time.

                if (!reader.TryReadUInt32BigEndian(out timescale))
                    return 0;

                if (!reader.TryReadUInt64BigEndian(out var longDuration))
                    return 0;

                duration = longDuration;
            }
            else
            {
                reader.Skip(8); // Creation and modification time.

                if (!reader.TryReadUInt32BigEndian(out timescale))
                    return 0;

                if (!reader.TryReadUInt32BigEndian(out var shortDuration))
                    return 0;

                duration = shortDuration;
            }

            return timescale > 0 ? duration / timescale : 0;
        }

        private static TrackInfo ReadTrack(Stream stream, Box trak)
        {
            var track = new TrackInfo();
            var trakBoxes = ReadBoxes(stream, trak.PayloadStart, trak.End);

            ReadTrackHeader(stream, trakBoxes, track);

            var mdia = FindBox(trakBoxes, "mdia");

            if (mdia == null)
                return track;

            var mdiaBoxes = ReadBoxes(stream, mdia.Value.PayloadStart, mdia.Value.End);

            ReadMediaHeader(stream, mdiaBoxes, track);
            ReadHandler(stream, mdiaBoxes, track);

            var minf = FindBox(mdiaBoxes, "minf");

            if (minf == null)
                return track;

            var stbl = FindBox(ReadBoxes(stream, minf.Value.PayloadStart, minf.Value.End), "stbl");

            if (stbl == null)
                return track;

            var stblBoxes = ReadBoxes(stream, stbl.Value.PayloadStart, stbl.Value.End);

            ReadSampleDescription(stream, stblBoxes, track);
            ReadTimeToSample(stream, stblBoxes, track);
            ReadSampleSizes(stream, stblBoxes, track);

            return track;
        }

        private static void ReadTrackHeader(Stream stream, List<Box> trakBoxes, TrackInfo track)
        {
            var tkhd = FindBox(trakBoxes, "tkhd");

            if (tkhd == null)
                return;

            var payload = ReadPayload(stream, tkhd.Value);

            if (payload == null)
                return;

            var reader = new MediaByteReader(payload);

            if (!reader.TryReadByte(out var version))
                return;

            reader.Skip(3); // Flags.
            reader.Skip(version == 1 ? 16 : 8); // Creation and modification time.
            reader.Skip(4); // Track id.
            reader.Skip(4); // Reserved.
            reader.Skip(version == 1 ? 8 : 4); // Duration.
            reader.Skip(8); // Reserved.
            reader.Skip(2); // Layer.
            reader.Skip(2); // Alternate group.
            reader.Skip(2); // Volume.
            reader.Skip(2); // Reserved.
            reader.Skip(36); // Transformation matrix.

            // Width and height are 16.16 fixed point here, unlike in the sample description.
            if (reader.TryReadUInt32BigEndian(out var width))
                track.Width = (int)Math.Round(width / 65536.0);

            if (reader.TryReadUInt32BigEndian(out var height))
                track.Height = (int)Math.Round(height / 65536.0);
        }

        private static void ReadMediaHeader(Stream stream, List<Box> mdiaBoxes, TrackInfo track)
        {
            var mdhd = FindBox(mdiaBoxes, "mdhd");

            if (mdhd == null)
                return;

            var payload = ReadPayload(stream, mdhd.Value);

            if (payload == null)
                return;

            var reader = new MediaByteReader(payload);

            if (!reader.TryReadByte(out var version))
                return;

            reader.Skip(3); // Flags.
            reader.Skip(version == 1 ? 16 : 8); // Creation and modification time.

            if (!reader.TryReadUInt32BigEndian(out var timescale))
                return;

            track.Timescale = timescale;

            double duration;

            if (version == 1)
            {
                if (!reader.TryReadUInt64BigEndian(out var longDuration))
                    return;

                duration = longDuration;
            }
            else
            {
                if (!reader.TryReadUInt32BigEndian(out var shortDuration))
                    return;

                duration = shortDuration;
            }

            if (timescale > 0)
                track.DurationSeconds = duration / timescale;
        }

        private static void ReadHandler(Stream stream, List<Box> mdiaBoxes, TrackInfo track)
        {
            var hdlr = FindBox(mdiaBoxes, "hdlr");

            if (hdlr == null)
                return;

            var payload = ReadPayload(stream, hdlr.Value);

            if (payload == null)
                return;

            var reader = new MediaByteReader(payload);

            reader.Skip(4); // Version and flags.
            reader.Skip(4); // Pre-defined.

            if (reader.TryReadFourCc(out var handler))
                track.Handler = handler;
        }

        private static void ReadSampleDescription(
            Stream stream,
            List<Box> stblBoxes,
            TrackInfo track
        )
        {
            var stsd = FindBox(stblBoxes, "stsd");

            if (stsd == null)
                return;

            var payload = ReadPayload(stream, stsd.Value);

            if (payload == null)
                return;

            var reader = new MediaByteReader(payload);

            reader.Skip(4); // Version and flags.

            if (!reader.TryReadUInt32BigEndian(out var entryCount) || entryCount == 0)
                return;

            var entryStart = reader.Position;

            if (!reader.TryReadUInt32BigEndian(out var entrySize))
                return;

            if (!reader.TryReadFourCc(out var format))
                return;

            track.CodecFourCc = format;

            reader.Skip(6); // Reserved.
            reader.Skip(2); // Data reference index.

            if (track.Handler == "soun")
            {
                reader.Skip(8); // Reserved.

                if (reader.TryReadUInt16BigEndian(out var channels))
                    track.Channels = channels;

                reader.Skip(2); // Sample size.
                reader.Skip(2); // Pre-defined.
                reader.Skip(2); // Reserved.

                // Sample rate is 16.16 fixed point, of which we only care about the whole part.
                if (reader.TryReadUInt16BigEndian(out var sampleRate))
                    track.SampleRate = sampleRate;

                return;
            }

            reader.Skip(2); // Pre-defined.
            reader.Skip(2); // Reserved.
            reader.Skip(12); // Pre-defined.

            // The sample description carries the coded resolution, which is more reliable than the
            // display resolution in the track header when a matrix or aspect ratio is applied.
            if (reader.TryReadUInt16BigEndian(out var codedWidth) && codedWidth > 0)
                track.Width = codedWidth;

            if (reader.TryReadUInt16BigEndian(out var codedHeight) && codedHeight > 0)
                track.Height = codedHeight;

            // Codec configuration boxes follow the 78 bytes of visual fields.
            var childStart = entryStart + 8 + 78;
            var childEnd = entrySize >= 8 ? entryStart + (int)entrySize : payload.Length;

            track.CodecProfile = ReadCodecProfile(
                payload,
                childStart,
                Math.Min(childEnd, payload.Length)
            );
        }

        private static string? ReadCodecProfile(byte[] payload, int start, int end)
        {
            if (start < 0 || start >= end)
                return null;

            var reader = new MediaByteReader(payload) { Position = start };

            while (reader.Position + 8 <= end)
            {
                var boxStart = reader.Position;

                if (!reader.TryReadUInt32BigEndian(out var size) || size < 8)
                    return null;

                if (!reader.TryReadFourCc(out var type))
                    return null;

                var boxEnd = boxStart + (int)size;

                if (boxEnd > end)
                    return null;

                switch (type)
                {
                    case "avcC" when reader.CanRead(4):
                        reader.Skip(1); // Configuration version.
                        reader.TryReadByte(out var profileIndication);
                        reader.Skip(1); // Profile compatibility.
                        reader.TryReadByte(out var levelIndication);

                        return MediaCodecNames.FormatAvcProfile(profileIndication, levelIndication);

                    case "hvcC" when reader.CanRead(13):
                        reader.Skip(1); // Configuration version.
                        reader.TryReadByte(out var profileSpaceTierIdc);
                        reader.Skip(10); // Compatibility flags and constraint indicators.
                        reader.TryReadByte(out var generalLevelIdc);

                        return MediaCodecNames.FormatHevcProfile(
                            profileSpaceTierIdc,
                            generalLevelIdc
                        );
                }

                reader.Position = boxEnd;
            }

            return null;
        }

        private static void ReadTimeToSample(Stream stream, List<Box> stblBoxes, TrackInfo track)
        {
            var stts = FindBox(stblBoxes, "stts");

            if (stts == null)
                return;

            var payload = ReadPayload(stream, stts.Value);

            if (payload == null)
                return;

            var reader = new MediaByteReader(payload);

            reader.Skip(4); // Version and flags.

            if (!reader.TryReadUInt32BigEndian(out var entryCount))
                return;

            if (entryCount > reader.Remaining / 8)
                return;

            long totalSamples = 0;
            long totalDuration = 0;
            var deltas = new HashSet<uint>();

            for (var i = 0; i < entryCount; ++i)
            {
                if (!reader.TryReadUInt32BigEndian(out var sampleCount))
                    break;

                if (!reader.TryReadUInt32BigEndian(out var sampleDelta))
                    break;

                totalSamples += sampleCount;
                totalDuration += (long)sampleCount * sampleDelta;

                // A trailing single-sample entry with an odd delta is normal even in constant frame
                // rate files, so it should not on its own count as variable frame rate.
                if (sampleCount > 1 || i < entryCount - 1)
                    deltas.Add(sampleDelta);
            }

            track.SampleCount = totalSamples;
            track.VariableFrameRate = deltas.Count > 1;

            if (track.Timescale > 0 && totalDuration > 0)
                track.SampleDurationSeconds = (double)totalDuration / track.Timescale;
        }

        private static void ReadSampleSizes(Stream stream, List<Box> stblBoxes, TrackInfo track)
        {
            var stsz = FindBox(stblBoxes, "stsz");

            if (stsz == null)
                return;

            var payload = ReadPayload(stream, stsz.Value);

            if (payload == null)
                return;

            var reader = new MediaByteReader(payload);

            reader.Skip(4); // Version and flags.

            if (!reader.TryReadUInt32BigEndian(out var sampleSize))
                return;

            if (!reader.TryReadUInt32BigEndian(out var sampleCount))
                return;

            if (sampleSize != 0)
            {
                track.TotalSampleBytes = (long)sampleSize * sampleCount;

                return;
            }

            if (sampleCount > reader.Remaining / 4)
                return;

            long total = 0;

            for (var i = 0; i < sampleCount; ++i)
            {
                if (!reader.TryReadUInt32BigEndian(out var size))
                    break;

                total += size;
            }

            track.TotalSampleBytes = total;
        }

        private static Box? FindBox(List<Box> boxes, string type)
        {
            foreach (var box in boxes)
                if (box.Type == type)
                    return box;

            return null;
        }

        /// <summary>
        /// Reads the box headers directly under the given range. Stops early rather than throwing
        /// whenever a size makes no sense, since that means the file is truncated or corrupt.
        /// </summary>
        private static List<Box> ReadBoxes(Stream stream, long start, long end)
        {
            var boxes = new List<Box>();
            var header = new byte[16];
            var position = start;

            while (position + 8 <= end && boxes.Count < MaxBoxesPerLevel)
            {
                stream.Position = position;

                try
                {
                    stream.ReadExactly(header, 0, 8);
                }
                catch (EndOfStreamException)
                {
                    break;
                }

                var size = (long)BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0, 4));
                var type = Encoding.ASCII.GetString(header, 4, 4);
                var headerSize = 8L;

                if (size == 1)
                {
                    if (position + 16 > end)
                        break;

                    try
                    {
                        stream.ReadExactly(header, 8, 8);
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }

                    size = (long)BinaryPrimitives.ReadUInt64BigEndian(header.AsSpan(8, 8));
                    headerSize = 16;
                }
                else if (size == 0)
                {
                    size = end - position;
                }

                if (size < headerSize || position + size > end)
                    break;

                boxes.Add(new Box(type, position + headerSize, position + size));
                position += size;
            }

            return boxes;
        }

        private static byte[]? ReadPayload(Stream stream, Box box)
        {
            var length = box.End - box.PayloadStart;

            if (length <= 0 || length > MaxPayloadBytes)
                return null;

            var payload = new byte[length];
            stream.Position = box.PayloadStart;

            try
            {
                stream.ReadExactly(payload, 0, payload.Length);
            }
            catch (EndOfStreamException)
            {
                return null;
            }

            return payload;
        }

        private readonly record struct Box(string Type, long PayloadStart, long End);

        private sealed class TrackInfo
        {
            public string Handler { get; set; } = string.Empty;
            public uint Timescale { get; set; }
            public double DurationSeconds { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string? CodecFourCc { get; set; }
            public string? CodecProfile { get; set; }
            public int Channels { get; set; }
            public int SampleRate { get; set; }
            public long SampleCount { get; set; }
            public double SampleDurationSeconds { get; set; }
            public bool VariableFrameRate { get; set; }
            public long TotalSampleBytes { get; set; }
        }
    }
}
