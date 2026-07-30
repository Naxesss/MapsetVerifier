using System.Buffers.Binary;
using System.Text;

namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    /// Bounds-checked cursor over an in-memory chunk of a media file. Every read returns false
    /// rather than throwing when the buffer is too short, since these buffers come from files that
    /// may well be truncated or corrupt.
    /// </summary>
    internal sealed class MediaByteReader(byte[] data)
    {
        public int Position { get; set; }

        public int Remaining => data.Length - Position;

        public bool CanRead(int count) => count >= 0 && Remaining >= count;

        public void Skip(int count) => Position += count;

        public bool TryReadByte(out byte value)
        {
            value = 0;

            if (!CanRead(1))
                return false;

            value = data[Position];
            Position += 1;

            return true;
        }

        public bool TryReadUInt16BigEndian(out ushort value) =>
            TryRead(2, out value, BinaryPrimitives.ReadUInt16BigEndian);

        public bool TryReadUInt32BigEndian(out uint value) =>
            TryRead(4, out value, BinaryPrimitives.ReadUInt32BigEndian);

        public bool TryReadUInt64BigEndian(out ulong value) =>
            TryRead(8, out value, BinaryPrimitives.ReadUInt64BigEndian);

        public bool TryReadDoubleBigEndian(out double value) =>
            TryRead(8, out value, BinaryPrimitives.ReadDoubleBigEndian);

        public bool TryReadUInt16LittleEndian(out ushort value) =>
            TryRead(2, out value, BinaryPrimitives.ReadUInt16LittleEndian);

        public bool TryReadUInt32LittleEndian(out uint value) =>
            TryRead(4, out value, BinaryPrimitives.ReadUInt32LittleEndian);

        /// <summary> Reads a four character code, e.g. "avc1". </summary>
        public bool TryReadFourCc(out string value) => TryReadAscii(4, out value);

        public bool TryReadAscii(int count, out string value)
        {
            value = string.Empty;

            if (!CanRead(count))
                return false;

            value = Encoding.ASCII.GetString(data, Position, count);
            Position += count;

            return true;
        }

        public bool TryReadUtf8(int count, out string value)
        {
            value = string.Empty;

            if (!CanRead(count))
                return false;

            value = Encoding.UTF8.GetString(data, Position, count);
            Position += count;

            return true;
        }

        private delegate T Converter<out T>(ReadOnlySpan<byte> source);

        private bool TryRead<T>(int size, out T value, Converter<T> convert)
        {
            value = default!;

            if (!CanRead(size))
                return false;

            value = convert(data.AsSpan(Position, size));
            Position += size;

            return true;
        }
    }
}
