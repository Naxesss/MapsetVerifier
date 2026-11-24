using System.Buffers.Binary;

namespace MapsetVerifier.Checks.Tests;

/// <summary> Builds small media files with exactly known properties, so no binaries need committing. </summary>
internal static class TestMedia
{
    private const int SampleRate = 44100;

    /// <summary> A png with the given resolution in its header, but hardly any image data. </summary>
    /// <param name="minimumSize"> Bytes to pad the file to with an unused chunk, if any. </param>
    public static byte[] Png(int width, int height, int minimumSize = 0)
    {
        using var stream = new MemoryStream();
        stream.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 2;

        Chunk(stream, "IHDR"u8, header);

        if (minimumSize > 0)
            Chunk(stream, "prVt"u8, new byte[minimumSize]);

        Chunk(stream, "IEND"u8, []);

        return stream.ToArray();
    }

    private static void Chunk(Stream stream, ReadOnlySpan<byte> type, byte[] data)
    {
        var number = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number);
        stream.Write(type);
        stream.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(number, Crc([.. type, .. data]));
        stream.Write(number);
    }

    private static uint Crc(byte[] data)
    {
        var crc = uint.MaxValue;

        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; ++bit)
                crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
        }

        return ~crc;
    }

    /// <summary> 16 bit PCM, with each channel's amplitude (0 to 1) given by the ms since the start. </summary>
    public static byte[] Wave(double milliseconds, params Func<double, double>[] channels)
    {
        var frames = (int)(milliseconds / 1000 * SampleRate);
        var dataSize = frames * channels.Length * 2;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
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
                // Alternates between extremes, so any stretch of it peaks at its amplitude.
                writer.Write(
                    (short)(
                        channel(i * 1000d / SampleRate) * short.MaxValue * (i % 2 == 0 ? 1 : -1)
                    )
                );

        return stream.ToArray();
    }

    /// <summary> A constant amplitude, for use with <see cref="Wave" />. </summary>
    public static Func<double, double> Level(double amplitude) => _ => amplitude;

    /// <summary> Silent mp3 frames of 128 kbps at 44.1 kHz, 26 ms each. </summary>
    public static byte[] Mp3(int frames = 100, byte bitrateIndex = 9)
    {
        int[] bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
        var frame = new byte[144 * bitrates[bitrateIndex] * 1000 / SampleRate];
        frame[0] = 0xFF;
        frame[1] = 0xFB;
        frame[2] = (byte)(bitrateIndex << 4);
        frame[3] = 0x44;

        return Enumerable.Repeat(frame, frames).SelectMany(bytes => bytes).ToArray();
    }

    /// <summary> An mp4 of nothing but headers, with video of the given codec and optionally sound. </summary>
    public static byte[] Mp4(string codec, bool sound = false)
    {
        byte[] Box(string type, params byte[][] parts)
        {
            var payload = parts.SelectMany(part => part).ToArray();
            var size = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(size, payload.Length + 8);

            return [.. size, .. System.Text.Encoding.ASCII.GetBytes(type), .. payload];
        }

        byte[] Track(string handler, string format) =>
            Box(
                "trak",
                Box(
                    "mdia",
                    Box("hdlr", new byte[8], System.Text.Encoding.ASCII.GetBytes(handler)),
                    Box(
                        "minf",
                        Box(
                            "stbl",
                            Box(
                                "stsd",
                                [0, 0, 0, 0, 0, 0, 0, 1],
                                Box(format, new byte[24], [5, 0, 2, 208], new byte[50])
                            )
                        )
                    )
                )
            );

        return
        [
            .. Box("ftyp", "isom"u8.ToArray(), new byte[4]),
            .. Box("moov", Track("vide", codec), sound ? Track("soun", "mp4a") : []),
        ];
    }
}
