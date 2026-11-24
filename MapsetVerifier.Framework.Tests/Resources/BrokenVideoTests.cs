using System.Buffers.Binary;
using System.Text;
using MapsetVerifier.Framework.Objects.Resources;
using Xunit;
using static MapsetVerifier.Framework.Tests.Resources.MediaFixtures;

namespace MapsetVerifier.Framework.Tests.Resources;

/// <summary> Videos which are cut off or damaged somewhere are read as far as possible. </summary>
public class BrokenVideoTests
{
    private static readonly string[] Containers = ["moov", "trak", "mdia", "minf", "stbl", "mvex"];

    private static readonly Dictionary<
        string,
        (byte[] File, Func<Stream, VideoMetadata?> Parse)
    > Videos = new()
    {
        ["mp4"] = (
            new Mp4Fixture
            {
                WithAudioTrack = true,
                WithMovieExtends = true,
                TimeToSample = [(10, 1000), (5, 900), (1, 10)],
            }.Build(),
            Mp4MetadataParser.Parse
        ),
        ["avi"] = (new AviFixture { WithAudioStream = true }.Build(), AviMetadataParser.Parse),
        ["flv"] = (new FlvFixture().Build(), FlvMetadataParser.Parse),
    };

    private static VideoMetadata? Parse(string type, byte[] file)
    {
        using var stream = new MemoryStream(file);
        var metadata = Videos[type].Parse(stream);

        if (metadata == null)
            return null;

        Assert.Equal(type.ToUpperInvariant(), metadata.Container);
        Assert.True(double.IsFinite(metadata.DurationMs) && metadata.DurationMs >= 0);
        Assert.True(metadata.FrameRate is null or > 0);

        return metadata;
    }

    [Theory]
    [InlineData("mp4")]
    [InlineData("avi")]
    [InlineData("flv")]
    public void CutOffAnywhere_IsReadAsFarAsPossible(string type)
    {
        var file = Videos[type].File;

        for (var length = 0; length < file.Length; length++)
            Parse(type, file[..length]);

        Assert.Equal(1280, Parse(type, file)!.Width);
    }

    /// <summary> As the reader is used by the app, where a damaged size may point anywhere. </summary>
    [Theory]
    [InlineData("mp4", 0xFF)]
    [InlineData("mp4", 0x80)]
    [InlineData("mp4", 0)]
    [InlineData("avi", 0xFF)]
    [InlineData("avi", 0)]
    [InlineData("flv", 0xFF)]
    [InlineData("flv", 0)]
    public void DamagedAnywhere_NeverThrows(string type, byte value)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mv-{Guid.NewGuid():N}.{type}");

        try
        {
            for (var i = 0; i < Videos[type].File.Length; i++)
            {
                var file = Videos[type].File.ToArray();
                file[i] = value;
                File.WriteAllBytes(path, file);

                Assert.NotNull(VideoMetadataReader.Read(path));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary> Every version of the file in which one box is cut short, with all sizes adjusted to it. </summary>
    private static IEnumerable<byte[]> WithShorterBoxes(byte[] boxes)
    {
        for (var start = 0; start + 8 <= boxes.Length; )
        {
            var size = (int)BinaryPrimitives.ReadUInt32BigEndian(boxes.AsSpan(start));
            var type = Encoding.ASCII.GetString(boxes, start + 4, 4);
            var (before, payload, after) = (
                boxes[..start],
                boxes[(start + 8)..(start + size)],
                boxes[(start + size)..]
            );

            if (Containers.Contains(type))
                foreach (var shorter in WithShorterBoxes(payload))
                    yield return Concat(before, Box(type, shorter), after);

            for (var length = 0; length < payload.Length; length++)
                yield return Concat(before, Box(type, payload[..length]), after);

            // And left out altogether.
            yield return Concat(before, after);

            start += size;
        }
    }

    [Fact]
    public void Mp4_WithAnyBoxCutShortOrMissing_IsReadAsFarAsPossible()
    {
        var read = 0;

        foreach (var file in WithShorterBoxes(Videos["mp4"].File))
            if (Parse("mp4", file) != null)
                read++;

        Assert.True(read > 100);
    }

    /// <summary> A movie whose headers use the long form, for dates and durations past 32 bits. </summary>
    private static byte[] LongForm(int timescale, int cut = 0)
    {
        byte[] Cut(byte[] payload) => payload[..^Math.Min(cut, payload.Length)];

        byte[] Header(int dates, byte[] rest) => Concat(U32(0x01000000), Zeros(dates), rest);

        return Concat(
            Box("ftyp", Ascii("isom")),
            Box(
                "moov",
                Box("mvhd", Cut(Header(16, Concat(U32(timescale), U64(5000))))),
                Box(
                    "trak",
                    Box(
                        "tkhd",
                        Header(
                            16,
                            Concat(Zeros(8 + 8 + 8 + 8 + 36), U32(640 * 65536), U32(480 * 65536))
                        )
                    ),
                    Box(
                        "mdia",
                        Box("mdhd", Cut(Header(16, Concat(U32(timescale), U64(4000))))),
                        Box("hdlr", Zeros(8), Ascii("vide"))
                    )
                )
            )
        );
    }

    [Fact]
    public void Mp4_WithLongHeaders()
    {
        var metadata = Parse("mp4", LongForm(1000))!;

        Assert.Equal((640, 480), (metadata.Width, metadata.Height));
        Assert.Equal(5000, metadata.DurationMs);
        Assert.Equal(0, Parse("mp4", LongForm(0))!.DurationMs);

        for (var cut = 1; cut < 40; cut++)
            Assert.Equal(640, Parse("mp4", LongForm(1000, cut))!.Width);
    }

    [Theory]
    [InlineData("hvcC", 1, 93, "Main@L3.1")]
    [InlineData("hvcC", 0x22, 120, "Main 10@L4 High tier")]
    [InlineData("hvcC", 3, 30, "Main Still Picture@L1")]
    public void Mp4_HevcProfile(string box, byte profile, byte level, string expected)
    {
        Assert.Equal(
            expected,
            Parse(
                "mp4",
                WithEntry(Box(box, Concat([1, profile], Zeros(10), [level])))
            )!.VideoCodecProfile
        );
    }

    [Fact]
    public void Mp4_UnreadableProfile_IsLeftOut()
    {
        byte[][] entries =
        [
            [],
            Zeros(4),
            Zeros(8), // A box without a size.
            Concat(U32(500), Ascii("avcC"), Zeros(4)), // Past the end.
            Box("avcC", Zeros(2)),
            Box("hvcC", Zeros(5)),
            Box("pasp", Zeros(8)), // Something else.
        ];

        foreach (var entry in entries)
            Assert.Null(Parse("mp4", WithEntry(entry))!.VideoCodecProfile);

        // After something else is fine.
        Assert.Equal(
            "Main@L3.1",
            Parse(
                "mp4",
                WithEntry(Concat(Box("pasp", Zeros(8)), Box("avcC", [1, 77, 0, 31])))
            )!.VideoCodecProfile
        );
    }

    /// <summary> With the given boxes in the description of the video, and one size per sample. </summary>
    private static byte[] WithEntry(byte[] boxes, byte[]? sizes = null) =>
        Concat(
            Box("ftyp", Ascii("isom")),
            Box(
                "moov",
                Box(
                    "trak",
                    Box(
                        "mdia",
                        Box("mdhd", Zeros(12), U32(1000), U32(2000)),
                        Box("hdlr", Zeros(8), Ascii("vide")),
                        Box(
                            "minf",
                            Box(
                                "stbl",
                                Box(
                                    "stsd",
                                    U32(0),
                                    U32(1),
                                    Box("hvc1", Zeros(24), U16(640), U16(480), Zeros(50), boxes)
                                ),
                                Box("stts", U32(0), U32(1), U32(2), U32(500)),
                                Box("stsz", sizes ?? Concat(U32(0), U32(0), U32(0)))
                            )
                        )
                    )
                )
            )
        );

    [Fact]
    public void Mp4_SampleSizes_AreAddedUp()
    {
        // Over one second.
        Assert.Equal(
            (100 + 150) * 8,
            Parse(
                "mp4",
                WithEntry([], Concat(U32(0), U32(0), U32(2), U32(100), U32(150)))
            )!.VideoBitrateBps
        );
        Assert.Equal(
            200 * 8,
            Parse("mp4", WithEntry([], Concat(U32(0), U32(100), U32(2))))!.VideoBitrateBps
        );

        // Fewer than it says.
        Assert.Null(
            Parse("mp4", WithEntry([], Concat(U32(0), U32(0), U32(9), U32(100))))!.VideoBitrateBps
        );
    }

    [Fact]
    public void Mp4_BoxUpToTheEndOfTheFile_HasNoSize()
    {
        var file = new Mp4Fixture().Build();
        var mdat = file.Length - 72;

        Assert.Equal("mdat", Encoding.ASCII.GetString(file, mdat + 4, 4));
        U32(0).CopyTo(file, mdat);

        Assert.Equal(1280, Parse("mp4", file)!.Width);
    }

    [Fact]
    public void Mp4_WithoutAVideoTrack_IsNotAVideo()
    {
        Assert.Null(
            Parse(
                "mp4",
                Box("moov", Box("trak", Box("mdia", Box("hdlr", Zeros(8), Ascii("soun")))))
            )
        );
        Assert.Null(Parse("mp4", Box("moov", Zeros(8))));
        Assert.Null(Parse("mp4", Box("ftyp", Zeros(8))));
    }

    private static byte[] Flv(byte flags, byte tagType, byte[] data) =>
        Concat(
            Ascii("FLV"),
            [1, flags],
            U32(9),
            U32(0),
            [tagType, 0, (byte)(data.Length >> 8), (byte)data.Length],
            Zeros(7),
            data
        );

    private static byte[] Key(string name) => Concat(U16(name.Length), Ascii(name));

    private static byte[] MetaData(byte type, params byte[][] properties) =>
        Concat([
            [2],
            Key("onMetaData"),
            [type],
            type == 8 ? U32(properties.Length) : [],
            .. properties,
        ]);

    private static byte[] Number(string key, double value) =>
        Concat(Key(key), [0], DoubleBe(value));

    [Fact]
    public void Flv_WithLittleToSay()
    {
        var metadata = Parse(
            "flv",
            Flv(4, 18, MetaData(3, Number("width", 320), Number("height", 240), Number("", 5)))
        )!;

        Assert.Equal((320, 240), (metadata.Width, metadata.Height));
        Assert.Null(metadata.FrameRate);
        Assert.Null(metadata.VideoBitrateBps);
        Assert.Null(metadata.VideoCodec);
        Assert.True(metadata.HasAudioTrack);
        Assert.Equal(1, metadata.AudioChannels);

        Assert.Equal(0, Parse("flv", Flv(0, 18, MetaData(3, Number("width", 320))))!.AudioChannels);
        Assert.Equal(240, Parse("flv", Flv(0, 18, MetaData(8, Number("height", 240))))!.Height);
    }

    [Fact]
    public void Flv_StringsAndFlagsAreSkipped_AndAnythingElseEndsIt()
    {
        var metadata = Parse(
            "flv",
            Flv(
                0,
                18,
                MetaData(
                    8,
                    Concat(Key("encoder"), [2], Key("Lavf")),
                    Concat(Key("stereo"), [1, 1]),
                    Concat(Key(""), [1, 1]),
                    Number("width", 320),
                    Concat(Key("keyframes"), [10], U32(0)),
                    Number("height", 240)
                )
            )
        )!;

        Assert.Equal((320, 0), (metadata.Width, metadata.Height));
        Assert.Equal(2, metadata.AudioChannels);
    }

    [Fact]
    public void Flv_WithoutItsMetadata_IsNotRead()
    {
        byte[][] files =
        [
            Flv(0, 18, MetaData(3, Number("duration", 5))), // Without a size.
            Flv(0, 9, Zeros(20)), // Only video.
            Flv(0, 18, Concat([1], Zeros(20))),
            Flv(0, 18, Concat([2], Key("onCuePoint"), [3], Zeros(20))),
            Flv(0, 18, Concat([2], Key("onMetaData"), [5], Zeros(20))),
            Concat(Ascii("FLV"), [1, 0], U32(5000), Zeros(20)),
        ];

        foreach (var file in files)
            Assert.Null(Parse("flv", file));

        // Found in a later tag.
        var wanted = Flv(0, 18, MetaData(3, Number("width", 320)));
        Assert.Equal(
            320,
            Parse("flv", Concat(Flv(0, 18, Concat([1], Zeros(20))), wanted[9..]))!.Width
        );
    }

    [Theory]
    [InlineData(40000u, 1u, 0u, 25)] // From the frame time.
    [InlineData(0u, 1u, 30u, 30)]
    [InlineData(0u, 0u, 30u, null)]
    public void Avi_FrameRate(uint microseconds, uint scale, uint rate, int? expected)
    {
        var metadata = Parse(
            "avi",
            new AviFixture
            {
                MicrosecondsPerFrame = microseconds,
                Scale = scale,
                Rate = rate,
            }.Build()
        )!;

        Assert.Equal(expected, metadata.FrameRate);
        Assert.Equal(expected == null ? 0 : 250_000d / expected.Value, metadata.DurationMs, 3);
    }

    [Theory]
    [InlineData("\0\0\0\0", "Uncompressed")]
    [InlineData("XVID", "MPEG-4 Visual")]
    public void Avi_Codec(string compression, string expected)
    {
        Assert.Equal(
            expected,
            Parse("avi", new AviFixture { Compression = compression }.Build())!.VideoCodec
        );
    }

    [Fact]
    public void Avi_WithoutASizeOrFrames()
    {
        Assert.Null(Parse("avi", new AviFixture { Width = 0, Height = 0 }.Build()));
        Assert.Equal(720, Parse("avi", new AviFixture { Width = 0 }.Build())!.Height);
        Assert.Equal(0, Parse("avi", new AviFixture { TotalFrames = 0 }.Build())!.DurationMs);
    }

    [Theory]
    [InlineData("video.xyz", "XYZ")]
    [InlineData("video", "Unknown")]
    public void UnknownContainer_IsNamedAfterItsExtension(string name, string expected)
    {
        var folder = Directory.CreateTempSubdirectory("mv-video-tests").FullName;
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, Zeros(50));

        try
        {
            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal(expected, metadata.Container);
            Assert.NotEmpty(metadata.Warnings);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void ByteReader_StopsAtTheEnd()
    {
        var reader = new MediaByteReader([65, 66, 67]);

        Assert.False(reader.CanRead(-1));
        Assert.False(reader.TryReadFourCc(out _));
        Assert.False(reader.TryReadUtf8(4, out _));
        Assert.False(reader.TryReadUInt32LittleEndian(out _));
        Assert.True(reader.TryReadAscii(3, out var text));
        Assert.Equal("ABC", text);
        Assert.False(reader.TryReadByte(out _));
    }

    private static byte[] Flv(int dataOffset, params byte[][] tags) =>
        Concat([Ascii("FLV"), [1, 5], U32(dataOffset), .. tags]);

    private static byte[] Tag(byte type, int length, params byte[][] data) =>
        Concat([U32(0), [type, 0, (byte)(length >> 8), (byte)length], Zeros(7), .. data]);

    private static byte[] Name(string name) => Concat(U16(name.Length), Ascii(name));

    private static byte[] Script(params byte[][] data)
    {
        var payload = Concat(data);

        return Tag(18, payload.Length, payload);
    }

    private static byte[] Property(string name, double value) =>
        Concat(Name(name), [0], DoubleBe(value));

    [Fact]
    public void Flv_PointingPastItsEnd_IsNotRead()
    {
        Assert.Null(Parse("flv", Flv(1000, Zeros(50))));
        // The tag's data too.
        Assert.Null(Parse("flv", Flv(9, Tag(18, 1000, Zeros(50)))));
    }

    [Fact]
    public void Flv_WithEmptyOrOtherScripts_IsReadFromTheNext()
    {
        var wanted = Script([2], Name("onMetaData"), [3], Property("width", 640), U16(0), [9]);

        Assert.Equal(
            640,
            Parse(
                "flv",
                Flv(
                    9,
                    Tag(9, 3, Zeros(3)),
                    Script([7]),
                    Script([2], Name("onTextData"), [3]),
                    // Neither an array nor an object.
                    Script([2], Name("onMetaData"), [1]),
                    wanted
                )
            )!.Width
        );
        Assert.Null(Parse("flv", Flv(9, Tag(18, 0))));
        Assert.Null(Parse("flv", Flv(9, Script([2], Name("onMetaData")))));
        Assert.Null(Parse("flv", Flv(9, Script([2], U16(50)))));
        Assert.Null(Parse("flv", Flv(9, Script([2]))));
    }

    /// <summary> What was read until then is kept. </summary>
    [Theory]
    [InlineData(new byte[] { 0, 5, 65 })] // Name cut off.
    [InlineData(new byte[] { 0, 1, 65 })] // Without a value.
    [InlineData(new byte[] { 0, 1, 65, 0, 1 })] // Number cut off.
    [InlineData(new byte[] { 0, 1, 65, 1 })] // Boolean.
    [InlineData(new byte[] { 0, 1, 65, 2, 0, 9 })] // Text.
    [InlineData(new byte[] { 0, 1, 65, 2, 0 })] // Its length.
    [InlineData(new byte[] { 0, 1, 65, 10, 0, 9 })] // Nested.
    public void Flv_CutOffWithinItsProperties_KeepsTheFirst(byte[] rest)
    {
        var metadata = Parse(
            "flv",
            Flv(9, Script([2], Name("onMetaData"), [3], Property("height", 480), rest))
        )!;

        Assert.Equal(480, metadata.Height);
        Assert.Equal(0, metadata.Width);
        Assert.Equal(1, metadata.AudioChannels);
    }

    [Fact]
    public void Flv_PropertiesWithoutANameOrText_AreSkipped()
    {
        Assert.Equal(
            720,
            Parse(
                "flv",
                Flv(
                    9,
                    Script(
                        [2],
                        Name("onMetaData"),
                        [3],
                        Property("", 5),
                        Concat(Name(""), [1, 1]),
                        Concat(Name("creator"), [2], Name("someone")),
                        Property("HEIGHT", 720)
                    )
                )
            )!.Height
        );
    }

    private static byte[] Avi(params byte[][] chunks)
    {
        var body = Concat(Ascii("AVI "), List("hdrl", chunks));

        return Concat(Ascii("RIFF"), U32Le(body.Length), body);
    }

    private static readonly byte[] AviVideo = Concat(
        Chunk("strh", Ascii("vids"), Zeros(40)),
        Chunk("strf", U32Le(40), U32Le(640), U32Le(480), Zeros(4), Zeros(4))
    );

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(4, 2, 0)]
    [InlineData(8, 2, 44100)]
    public void Avi_WithItsAudioCutShort_IsReadAsFarAsPossible(int length, int channels, int rate)
    {
        var format = Concat(U16Le(0x55), U16Le(2), U32Le(44100))[..length];
        var metadata = Parse(
            "avi",
            Avi(AviVideo, Chunk("strh", Ascii("auds"), Zeros(40)), Chunk("strf", format))
        )!;

        Assert.Equal((640, 480), (metadata.Width, metadata.Height));
        Assert.Equal("Uncompressed", metadata.VideoCodec);
        Assert.Equal(channels, metadata.AudioChannels);
        Assert.Equal(rate, metadata.AudioSampleRate);
        Assert.Equal(length > 0, metadata.HasAudioTrack);
    }

    [Theory]
    [InlineData(0, 640, 480)]
    [InlineData(4, 640, 480)]
    [InlineData(32, 640, 480)]
    [InlineData(36, 1, 480)]
    [InlineData(40, 1, 1)]
    public void Avi_WithItsMainHeaderCutShort_IsReadAsFarAsPossible(
        int length,
        int width,
        int height
    )
    {
        var header = Concat(Zeros(32), U32Le(1), U32Le(1))[..length];
        var metadata = Parse("avi", Avi(AviVideo, Chunk("avih", header)))!;

        Assert.Equal((width, height), (metadata.Width, metadata.Height));
    }

    [Theory]
    [InlineData(4, 0, 0)]
    [InlineData(8, 640, 0)]
    [InlineData(12, 640, 480)]
    public void Avi_WithItsVideoCutShort_IsReadAsFarAsPossible(int length, int width, int height)
    {
        var format = Concat(U32Le(40), U32Le(640), U32Le(480))[..length];
        var metadata = Parse(
            "avi",
            Avi(Chunk("strh", Ascii("vids"), Zeros(40)), Chunk("strf", format))
        );

        Assert.Equal((width, height), (metadata?.Width ?? 0, metadata?.Height ?? 0));
        Assert.Null(metadata?.FrameRate);
    }
}
