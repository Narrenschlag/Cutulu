namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// A simple wrapper for binary data that allows for easy reading and writing of bits and bytes.
    /// </summary>
    public class ByteBuilder
    {
        private readonly List<byte> _buffer;
        private int _writeBitPosition;
        private int _readBitPosition;

        /// <summary>
        /// Creates a new BitBuilder.
        /// </summary>
        public ByteBuilder(object value = null)
        {
            _buffer = value == null ? [] : new List<byte>(value.Encode());
            _writeBitPosition = _buffer.Count * 8;
            _readBitPosition = 0;
        }

        /// <summary>
        /// Returns the buffer as a byte array.
        /// </summary>
        public byte[] Buffer => [.. _buffer];

        public int BitLength => _buffer.Count * 8;
        public int ByteLength => _buffer.Count;

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
            _buffer.AddRange(data);
            _writeBitPosition += data.Length * 8;
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
                var byteIndex = _writeBitPosition / 8;
                var bitIndex = _writeBitPosition % 8;

                if (byteIndex >= _buffer.Count)
                {
                    _buffer.Add(0);
                }

                if (bit)
                {
                    _buffer[byteIndex] |= (byte)(1 << (7 - bitIndex));
                }

                _writeBitPosition++;
            }
        }

        /// <summary>
        /// Reads the stream into a byte array.
        /// </summary>
        public byte[] Read(int length)
        {
            if (length > _buffer.Count - (_readBitPosition / 8))
                throw new ArgumentOutOfRangeException(nameof(length), "Not enough bytes available.");

            var result = _buffer.GetRange(_readBitPosition / 8, length).ToArray();
            _readBitPosition += length * 8;
            return result;
        }

        /// <summary>
        /// Reads individual bits from the buffer.
        /// </summary>
        public bool[] ReadBits(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");

            var availableBits = _buffer.Count * 8 - _readBitPosition;
            if (length > availableBits)
                throw new ArgumentOutOfRangeException(nameof(length), $"Not enough bits available. Requested: {length}, Available: {availableBits}");

            var result = new bool[length];
            for (int i = 0; i < length; i++)
            {
                var byteIndex = _readBitPosition / 8;
                var bitIndex = _readBitPosition % 8;
                result[i] = (_buffer[byteIndex] & (1 << (7 - bitIndex))) != 0;
                _readBitPosition++;
            }

            // Remove fully read bytes
            var bytesToRemove = _readBitPosition / 8;
            if (bytesToRemove > 0)
            {
                _buffer.RemoveRange(0, bytesToRemove);
                _readBitPosition %= 8;
            }

            return result;
        }
    }
}