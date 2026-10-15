using System.Buffers.Binary;
using System.Text;

namespace MapsetVerifier.Framework.Tests.Resources
{
    /// <summary>
    /// Builds the smallest valid container files that still carry the headers the parsers read, so
    /// the tests do not need committed binaries.
    /// </summary>
    internal static class MediaFixtures
    {
        public static byte[] U16(int value)
        {
            var bytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)value);

            return bytes;
        }

        public static byte[] U32(long value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)value);

            return bytes;
        }

        public static byte[] U64(long value)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, (ulong)value);

            return bytes;
        }

        public static byte[] U16Le(int value)
        {
            var bytes = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)value);

            return bytes;
        }

        public static byte[] U32Le(long value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)value);

            return bytes;
        }

        public static byte[] DoubleBe(double value)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteDoubleBigEndian(bytes, value);

            return bytes;
        }

        public static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);

        public static byte[] Zeros(int count) => new byte[count];

        public static byte[] Concat(params byte[][] parts)
        {
            var result = new byte[parts.Sum(part => part.Length)];
            var offset = 0;

            foreach (var part in parts)
            {
                part.CopyTo(result, offset);
                offset += part.Length;
            }

            return result;
        }

        /// <summary> Wraps the given payload in an ISO base media box. </summary>
        public static byte[] Box(string type, params byte[][] parts)
        {
            var payload = Concat(parts);

            return Concat(U32(payload.Length + 8), Ascii(type), payload);
        }

        /// <summary> Wraps the given payload in a box using the 64 bit size form. </summary>
        public static byte[] LargeBox(string type, params byte[][] parts)
        {
            var payload = Concat(parts);

            return Concat(U32(1), Ascii(type), U64(payload.Length + 16), payload);
        }

        /// <summary> Wraps the given payload in a RIFF chunk, padded to an even length. </summary>
        public static byte[] Chunk(string type, params byte[][] parts)
        {
            var payload = Concat(parts);
            var padding = payload.Length % 2 == 0 ? Array.Empty<byte>() : new byte[1];

            return Concat(Ascii(type), U32Le(payload.Length), payload, padding);
        }

        public static byte[] List(string listType, params byte[][] parts) =>
            Chunk("LIST", Concat(Ascii(listType), Concat(parts)));
    }
}
