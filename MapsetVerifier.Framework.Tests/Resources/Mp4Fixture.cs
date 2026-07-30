using static MapsetVerifier.Framework.Tests.Resources.MediaFixtures;

namespace MapsetVerifier.Framework.Tests.Resources
{
    /// <summary> Assembles a minimal but structurally valid MP4 out of header boxes only. </summary>
    internal sealed class Mp4Fixture
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public uint MovieTimescale { get; set; } = 1000;
        public uint MovieDuration { get; set; } = 5000;
        public uint MediaTimescale { get; set; } = 30000;

        /// <summary> Sample count and delta pairs of the time to sample table. </summary>
        public List<(uint Count, uint Delta)> TimeToSample { get; set; } = [(150, 1000)];

        public uint SampleSize { get; set; } = 1000;
        public string VideoFormat { get; set; } = "avc1";

        /// <summary> Profile and level indications of the AVC configuration, when present. </summary>
        public (byte Profile, byte Level)? AvcConfiguration { get; set; } = (100, 40);

        public bool WithAudioTrack { get; set; }
        public bool WithMovieExtends { get; set; }
        public bool UseLargeMoovBox { get; set; }

        public byte[] Build()
        {
            var moovParts = new List<byte[]> { BuildMovieHeader(), BuildVideoTrack() };

            if (WithMovieExtends)
                moovParts.Add(Box("mvex", Box("trex", Zeros(24))));

            if (WithAudioTrack)
                moovParts.Add(BuildAudioTrack());

            var moov = UseLargeMoovBox
                ? LargeBox("moov", moovParts.ToArray())
                : Box("moov", moovParts.ToArray());

            return Concat(
                Box("ftyp", Ascii("isom"), U32(512), Ascii("isom"), Ascii("mp41")),
                moov,
                Box("mdat", Zeros(64))
            );
        }

        private byte[] BuildMovieHeader() =>
            Box(
                "mvhd",
                U32(0), // Version and flags.
                U32(0), // Creation time.
                U32(0), // Modification time.
                U32(MovieTimescale),
                U32(MovieDuration),
                U32(0x00010000), // Rate.
                U16(0x0100), // Volume.
                Zeros(2), // Reserved.
                Zeros(8), // Reserved.
                Zeros(36), // Transformation matrix.
                Zeros(24), // Pre-defined.
                U32(2) // Next track id.
            );

        private byte[] BuildVideoTrack() =>
            Box(
                "trak",
                BuildTrackHeader(Width, Height),
                Box(
                    "mdia",
                    BuildMediaHeader(
                        MediaTimescale,
                        MediaTimescale * MovieDuration / MovieTimescale
                    ),
                    BuildHandler("vide"),
                    Box(
                        "minf",
                        Box(
                            "stbl",
                            BuildVideoSampleDescription(),
                            BuildTimeToSample(),
                            BuildSampleSizes()
                        )
                    )
                )
            );

        private byte[] BuildAudioTrack() =>
            Box(
                "trak",
                BuildTrackHeader(0, 0),
                Box(
                    "mdia",
                    BuildMediaHeader(44100, 44100 * MovieDuration / MovieTimescale),
                    BuildHandler("soun"),
                    Box("minf", Box("stbl", BuildAudioSampleDescription()))
                )
            );

        private static byte[] BuildTrackHeader(int width, int height) =>
            Box(
                "tkhd",
                U32(0x00000007), // Version zero, enabled and in both preview and movie.
                U32(0), // Creation time.
                U32(0), // Modification time.
                U32(1), // Track id.
                Zeros(4), // Reserved.
                U32(0), // Duration.
                Zeros(8), // Reserved.
                U16(0), // Layer.
                U16(0), // Alternate group.
                U16(0), // Volume.
                Zeros(2), // Reserved.
                Zeros(36), // Transformation matrix.
                U32(width * 65536L), // Width as 16.16 fixed point.
                U32(height * 65536L) // Height as 16.16 fixed point.
            );

        private static byte[] BuildMediaHeader(uint timescale, uint duration) =>
            Box(
                "mdhd",
                U32(0), // Version and flags.
                U32(0), // Creation time.
                U32(0), // Modification time.
                U32(timescale),
                U32(duration),
                U16(0x55C4), // Language.
                U16(0) // Pre-defined.
            );

        private static byte[] BuildHandler(string handlerType) =>
            Box(
                "hdlr",
                U32(0), // Version and flags.
                U32(0), // Pre-defined.
                Ascii(handlerType),
                Zeros(12), // Reserved.
                Zeros(1) // Empty name.
            );

        private byte[] BuildVideoSampleDescription()
        {
            var configuration = AvcConfiguration is { } avc
                ? Box("avcC", new byte[] { 1, avc.Profile, 0, avc.Level })
                : Array.Empty<byte>();

            var entry = Box(
                VideoFormat,
                Zeros(6), // Reserved.
                U16(1), // Data reference index.
                U16(0), // Pre-defined.
                U16(0), // Reserved.
                Zeros(12), // Pre-defined.
                U16(Width),
                U16(Height),
                U32(0x00480000), // Horizontal resolution.
                U32(0x00480000), // Vertical resolution.
                Zeros(4), // Reserved.
                U16(1), // Frame count.
                Zeros(32), // Compressor name.
                U16(24), // Depth.
                U16(0xFFFF), // Pre-defined.
                configuration
            );

            return Box("stsd", U32(0), U32(1), entry);
        }

        private static byte[] BuildAudioSampleDescription()
        {
            var entry = Box(
                "mp4a",
                Zeros(6), // Reserved.
                U16(1), // Data reference index.
                Zeros(8), // Reserved.
                U16(2), // Channel count.
                U16(16), // Sample size.
                U16(0), // Pre-defined.
                U16(0), // Reserved.
                U16(44100), // Sample rate, whole part of 16.16 fixed point.
                U16(0) // Sample rate, fractional part.
            );

            return Box("stsd", U32(0), U32(1), entry);
        }

        private byte[] BuildTimeToSample()
        {
            var entries = TimeToSample
                .SelectMany(entry => new[] { U32(entry.Count), U32(entry.Delta) })
                .ToArray();

            return Box("stts", U32(0), U32(TimeToSample.Count), Concat(entries));
        }

        private byte[] BuildSampleSizes()
        {
            var sampleCount = TimeToSample.Sum(entry => (long)entry.Count);

            return Box("stsz", U32(0), U32(SampleSize), U32(sampleCount));
        }
    }
}
