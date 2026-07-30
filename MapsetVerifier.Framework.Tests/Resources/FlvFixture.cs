using static MapsetVerifier.Framework.Tests.Resources.MediaFixtures;

namespace MapsetVerifier.Framework.Tests.Resources
{
    /// <summary> Assembles a minimal FLV file carrying a single "onMetaData" script tag. </summary>
    internal sealed class FlvFixture
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public double DurationSeconds { get; set; } = 10;
        public double FrameRate { get; set; } = 30;
        public double VideoDataRateKbps { get; set; } = 800;
        public int VideoCodecId { get; set; } = 7;
        public bool WithAudio { get; set; } = true;

        public byte[] Build()
        {
            var properties = new List<byte[]>
            {
                Number("duration", DurationSeconds),
                Number("width", Width),
                Number("height", Height),
                Number("framerate", FrameRate),
                Number("videodatarate", VideoDataRateKbps),
                Number("videocodecid", VideoCodecId),
            };

            if (WithAudio)
            {
                properties.Add(Number("audiocodecid", 10));
                properties.Add(Number("audiosamplerate", 44100));
                properties.Add(Boolean("stereo", true));
            }

            var scriptData = Concat([
                new byte[] { 0x02 },
                AmfString("onMetaData"),
                new byte[] { 0x08 },
                U32(properties.Count),
                .. properties,
                U16(0),
                new byte[] { 0x09 },
            ]);

            var flags = (byte)(0x01 | (WithAudio ? 0x04 : 0x00));

            return Concat(
                Ascii("FLV"),
                new byte[] { 0x01, flags },
                U32(9),
                U32(0), // Size of the previous tag.
                new byte[] { 18 },
                UInt24(scriptData.Length),
                UInt24(0), // Timestamp.
                new byte[] { 0 }, // Timestamp extension.
                UInt24(0), // Stream id.
                scriptData
            );
        }

        private static byte[] Number(string key, double value) =>
            Concat(AmfString(key), new byte[] { 0x00 }, DoubleBe(value));

        private static byte[] Boolean(string key, bool value) =>
            Concat(AmfString(key), new byte[] { 0x01, value ? (byte)1 : (byte)0 });

        private static byte[] AmfString(string value) => Concat(U16(value.Length), Ascii(value));

        private static byte[] UInt24(int value) =>
            [(byte)(value >> 16), (byte)(value >> 8), (byte)value];
    }
}
