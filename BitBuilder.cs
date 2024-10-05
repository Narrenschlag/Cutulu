namespace Cutulu
{
    using System.Collections.Generic;
    using System.Collections;
    using System;
    using Godot;

    /// <summary>
    /// A simple wrapper for binary data that allows for easy reading and writing of bits and bytes.
    /// </summary>
    public readonly struct BitBuilder : IEnumerable<bool>
    {
        private readonly List<bool> buffer;

        /// <summary>
        /// Creates a new BitBuilder.
        /// </summary>
        public BitBuilder(object value = null)
        {
            buffer = new();

            Add(value);
        }

        // Indexer to make 'Index' act like an array
        public bool this[int index]
        {
            get { return buffer[index]; }  // Get value at the given index
            set { buffer[index] = value; } // Set value at the given index
        }

        /// <summary>
        /// Returns the buffer as a bit array.
        /// </summary>
        public bool[] BitBuffer => buffer.ToArray();

        /// <summary>
        /// Returns the buffer as a byte array.
        /// </summary>
        public byte[] ByteBuffer
        {
            get
            {
                var result = new byte[ByteLength];

                for (var i = 0; i < buffer.Count; i++)
                {
                    if (buffer[i])
                    {
                        var byteIndex = i / 8;
                        var bitIndex = (byte)(i % 8);

                        Bitf.EnableBit(ref result[byteIndex], ref bitIndex);
                    }
                }

                return result;
            }
        }

        public int ByteLength => Mathf.CeilToInt(buffer.Count / 8.0f);
        public int Length => buffer.Count;

        public IEnumerator<bool> GetEnumerator()
        {
            return buffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a binary pattern of bits to the buffer.
        /// 00000001 is a byte with value 1.
        /// Every non 0 value is considered as '1'.
        /// </summary>
        public void AddBinary(string pattern)
        {
            if (pattern.IsEmpty()) return;

            for (var i = pattern.Length - 1; i >= 0; i--)
            {
                Add(pattern[i]);
            }
        }

        public string ReadBinary(int position, int length)
        {
            var result = string.Empty;

            for (var i = 0; i < length; i++)
            {
                result += position < Length && this[position + i] ? '1' : '0';
            }

            return result;
        }

        public void Fill(int targetLength, bool value)
        {
            for (var i = 0; i < targetLength - Length; i++)
            {
                buffer.Add(value);
            }
        }

        public void Add(int length, bool value)
        {
            for (var i = 0; i < length; i++)
            {
                buffer.Add(value);
            }
        }

        public void Add(object value)
        {
            var bytes = value == null ? Array.Empty<byte>() : value.Encode();

            for (var i = 0; i < bytes.Length; i++)
            {
                for (var k = 0; k < 8; k++)
                {
                    // Add bits to buffer
                    buffer.Add((bytes[i] & (1 << (7 - k))) != 0);
                }
            }
        }

        public void Add(bool value)
        {
            buffer.Add(value);
        }

        public void Insert(int index, bool value)
        {
            buffer.Insert(index, value);
        }

        public void RemoveAt(int index)
        {
            buffer.RemoveAt(index);
        }

        public void SetRange(int index, int length, params bool[] values)
        {
            for (var i = 0; i < length;)
            {
                for (var k = 0; k < values.Length && i < length; k++, i++)
                {
                    this[index + i] = values[k];
                }
            }
        }

        public byte GetByte(int startIndex, int length)
        {
            if (length > 8) throw new($"Cannot get byte with length greater than 8.");

            var result = default(byte);
            var k = default(byte);

            for (int i = startIndex; i < startIndex + length && i < Length; i++, k++)
            {
                if (this[i])
                    Bitf.EnableBit(ref result, ref k);
            }

            return result;
        }
    }
}