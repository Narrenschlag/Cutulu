namespace Cutulu
{
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// A simple wrapper for binary data that allows for easy reading and writing of bits and bytes.
    /// </summary>
    public class ByteBuilder
    {
        private int writeBitPosition;
        private int readBitPosition;
        private List<byte> buffer;

        /// <summary>
        /// Creates a new BitBuilder.
        /// </summary>
        public ByteBuilder(object value = null)
        {
            buffer = new List<byte>(value == null ? Array.Empty<byte>() : value.Encode());
            writeBitPosition = buffer.Count * 8;
            readBitPosition = 0;
        }

        /// <summary>
        /// Returns the buffer as a byte array.
        /// </summary>
        public byte[] Buffer => buffer.ToArray();

        public int BitLength => buffer.Count * 8;
        public int ByteLength => buffer.Count;

        /// <summary>
        /// Returns buffer as given type T.
        /// </summary>
        public bool TryGetValue<T>(out T value) => Buffer.TryDecode(out value);

        /// <summary>
        /// Returns buffer as given type T.
        /// </summary>
        public T GetValue<T>() => Buffer.Decode<T>();

        /// <summary>
        /// Writes the buffer to the stream.
        /// </summary>
        public void Write(byte[] data)
        {
            buffer.AddRange(data);
            writeBitPosition += data.Length * 8;
        }

        /// <summary>
        /// Encodes object and writes the buffer to the stream.
        /// </summary>
        public void Write(object value) => Write(value.Encode());

        /// <summary>
        /// Writes individual bits to the buffer.
        /// </summary>
        public void WriteBits(params bool[] bits)
        {
            foreach (bool bit in bits)
            {
                var byteIndex = writeBitPosition / 8;
                var bitIndex = writeBitPosition % 8;

                if (byteIndex >= buffer.Count)
                {
                    buffer.Add(0);
                }

                if (bit)
                {
                    buffer[byteIndex] |= (byte)(1 << (7 - bitIndex));
                }

                writeBitPosition++;
            }
        }

        /// <summary>
        /// Reads the stream into a byte array.
        /// </summary>
        public byte[] Read(int length)
        {
            if (length > buffer.Count - (readBitPosition / 8))
                throw new ArgumentOutOfRangeException(nameof(length), "Not enough bytes available.");

            var result = buffer.GetRange(readBitPosition / 8, length).ToArray();
            readBitPosition += length * 8;
            return result;
        }

        /// <summary>
        /// Reads individual bits from the buffer.
        /// </summary>
        public bool[] ReadBits(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");

            var availableBits = buffer.Count * 8 - readBitPosition;
            if (length > availableBits)
                throw new ArgumentOutOfRangeException(nameof(length), $"Not enough bits available. Requested: {length}, Available: {availableBits}");

            var result = new bool[length];
            for (int i = 0; i < length; i++)
            {
                var byteIndex = readBitPosition / 8;
                var bitIndex = readBitPosition % 8;
                result[i] = (buffer[byteIndex] & (1 << (7 - bitIndex))) != 0;
                readBitPosition++;
            }

            // Remove fully read bytes
            var bytesToRemove = readBitPosition / 8;
            if (bytesToRemove > 0)
            {
                buffer.RemoveRange(0, bytesToRemove);
                readBitPosition %= 8;
            }

            return result;
        }
    }
}