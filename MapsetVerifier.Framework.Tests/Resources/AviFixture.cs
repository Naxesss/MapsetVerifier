using static MapsetVerifier.Framework.Tests.Resources.MediaFixtures;

namespace MapsetVerifier.Framework.Tests.Resources
{
    /// <summary> Assembles a minimal RIFF/AVI file consisting only of its header list. </summary>
    internal sealed class AviFixture
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public uint MicrosecondsPerFrame { get; set; } = 40000;
        public uint TotalFrames { get; set; } = 250;
        public uint Scale { get; set; } = 1;
        public uint Rate { get; set; } = 25;
        public string Compression { get; set; } = "XVID";
        public bool WithAudioStream { get; set; }

        public byte[] Build()
        {
            var streams = new List<byte[]>
            {
                List("strl", BuildStreamHeader("vids"), BuildVideoFormat()),
            };

            if (WithAudioStream)
                streams.Add(List("strl", BuildStreamHeader("auds"), BuildAudioFormat()));

            var body = Concat(
                Ascii("AVI "),
                List("hdrl", Concat([BuildAviHeader(), .. streams])),
                List("movi", Chunk("00dc", Zeros(32)))
            );

            return Concat(Ascii("RIFF"), U32Le(body.Length), body);
        }

        private byte[] BuildAviHeader() =>
            Chunk(
                "avih",
                U32Le(MicrosecondsPerFrame),
                U32Le(0), // Max bytes per second.
                U32Le(0), // Padding granularity.
                U32Le(0x10), // Flags.
                U32Le(TotalFrames),
                U32Le(0), // Initial frames.
                U32Le(WithAudioStream ? 2 : 1), // Stream count.
                U32Le(0), // Suggested buffer size.
                U32Le(Width),
                U32Le(Height),
                Zeros(16) // Reserved.
            );

        private byte[] BuildStreamHeader(string streamType) =>
            Chunk(
                "strh",
                Ascii(streamType),
                Ascii(streamType == "vids" ? Compression : "\0\0\0\0"),
                U32Le(0), // Flags.
                U16Le(0), // Priority.
                U16Le(0), // Language.
                U32Le(0), // Initial frames.
                U32Le(streamType == "vids" ? Scale : 1),
                U32Le(streamType == "vids" ? Rate : 44100),
                U32Le(0), // Start.
                U32Le(streamType == "vids" ? TotalFrames : 441000),
                U32Le(0), // Suggested buffer size.
                U32Le(0), // Quality.
                U32Le(0), // Sample size.
                Zeros(8) // Frame rectangle.
            );

        private byte[] BuildVideoFormat() =>
            Chunk(
                "strf",
                U32Le(40), // Structure size.
                U32Le(Width),
                U32Le(Height),
                U16Le(1), // Planes.
                U16Le(24), // Bit count.
                Ascii(Compression),
                U32Le(0), // Image size.
                U32Le(0), // Horizontal pixels per meter.
                U32Le(0), // Vertical pixels per meter.
                U32Le(0), // Colours used.
                U32Le(0) // Colours important.
            );

        private static byte[] BuildAudioFormat() =>
            Chunk(
                "strf",
                U16Le(0x0055), // Format tag, MP3.
                U16Le(2), // Channels.
                U32Le(44100), // Samples per second.
                U32Le(16000), // Average bytes per second.
                U16Le(1), // Block align.
                U16Le(16), // Bits per sample.
                U16Le(0) // Extra size.
            );
    }
}
