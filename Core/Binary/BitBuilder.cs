namespace Cutulu.Core;

using System.Collections.Generic;
using System.Collections;
using System;

/// <summary>
/// A simple wrapper for binary data that allows for easy reading and writing of bits and bytes.
/// </summary>
public readonly struct BitBuilder : IEnumerable<bool>
{
    private readonly List<bool> Buffer;

    public BitBuilder() => Buffer = [];

    /// <summary>
    /// Creates a new BitBuilder.
    /// </summary>
    public BitBuilder(object value = null) : this()
    {
        Add(value);
    }

    // Indexer to make 'Index' act like an array
    public bool this[int index]
    {
        get { return Buffer[index]; }  // Get value at the given index
        set { Buffer[index] = value; } // Set value at the given index
    }

    /// <summary>
    /// Returns the buffer as a bit array.
    /// </summary>
    public bool[] BitBuffer => [.. Buffer];

    /// <summary>
    /// Returns the buffer as a byte array.
    /// </summary>
    public byte[] ByteBuffer
    {
        get
        {
            var _buffer = new byte[ByteLength];

            for (var i = 0; i < Buffer.Count; i++)
            {
                if (Buffer[i])
                {
                    var _bit_index = (byte)(i % 8);
                    var _byte_index = i / 8;

                    _buffer[_byte_index] = Bitf.EnableBit(_buffer[_byte_index], _bit_index);
                }
            }

            return _buffer;
        }
    }

    public int ByteLength => (int)Math.Ceiling(Buffer.Count / 8.0f);
    public int Length => Buffer.Count;

    public IEnumerator<bool> GetEnumerator()
    {
        return Buffer.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Clear()
    {
        Buffer.Clear();
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
            Add(value);
        }
    }

    public void Add(int length, bool value)
    {
        for (var i = 0; i < length; i++)
        {
            Add(value);
        }
    }

    public void Add(object value)
    {
        var _bytes = value == null ? [] : value.Encode();

        for (var i = 0; i < _bytes.Length; i++)
        {
            for (var k = 0; k < 8; k++)
            {
                // Add bits to buffer
                Buffer.Add((_bytes[i] & (1 << (7 - k))) != 0);
            }
        }
    }

    public void Add(bool value)
    {
        Buffer.Add(value);
    }

    public void Insert(int index, bool value)
    {
        Buffer.Insert(index, value);
    }

    public void RemoveAt(int index)
    {
        Buffer.RemoveAt(index);
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
            if (this[i]) result = Bitf.EnableBit(result, k);
        }

        return result;
    }

    /// <summary>
    /// Sets or clears a specific bit in the given integer type.
    /// </summary>
    /// <typeparam name="T">The integer type (e.g., byte, int, long).</typeparam>
    /// <param name="obj">The original value to modify.</param>
    /// <param name="bitIndex">The bit position to set or clear (0-based).</param>
    /// <param name="value">If true, sets the bit; if false, clears it.</param>
    /// <returns>The modified value with the bit set or cleared as specified.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type is not supported.</exception>
    public static T SetBit<T>(T obj, int bitIndex, bool value) where T : unmanaged
    {
        switch (obj)
        {
            case sbyte sb:
                return (T)(object)(value ? (sbyte)(sb | (sbyte)(1 << bitIndex)) : (sbyte)(sb & (sbyte)~(1 << bitIndex)));

            case byte b:
                return (T)(object)(value ? (byte)(b | (byte)(1 << bitIndex)) : (byte)(b & (byte)~(1 << bitIndex)));

            case short s:
                return (T)(object)(value ? (short)(s | (short)(1 << bitIndex)) : (short)(s & (short)~(1 << bitIndex)));

            case ushort us:
                return (T)(object)(value ? (ushort)(us | (ushort)(1 << bitIndex)) : (ushort)(us & (ushort)~(1 << bitIndex)));

            case int i:
                return (T)(object)(value ? (i | (1 << bitIndex)) : (i & ~(1 << bitIndex)));

            case uint ui:
                return (T)(object)(value ? (ui | (1u << bitIndex)) : (ui & ~(1u << bitIndex)));

            case long l:
                return (T)(object)(value ? (l | (1L << bitIndex)) : (l & ~(1L << bitIndex)));

            case ulong ul:
                return (T)(object)(value ? (ul | (1ul << bitIndex)) : (ul & ~(1ul << bitIndex)));

            default:
                dynamic dynamicObj = obj;
                return (T)(value ? dynamicObj | (dynamic)(1 << bitIndex) : (dynamicObj & ~(dynamic)(1 << bitIndex)));
                //throw new NotSupportedException($"Type {typeof(T)} is not supported for bit operations.");
        }
    }

    /// <summary>
    /// Retrieves the value of a specific bit in the given integer type.
    /// </summary>
    /// <typeparam name="T">The integer type (e.g., byte, int, long).</typeparam>
    /// <param name="obj">The value from which to retrieve the bit.</param>
    /// <param name="bitIndex">The bit position to check (0-based).</param>
    /// <returns>True if the specified bit is set, false otherwise.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type is not supported.</exception>
    public static bool GetBit<T>(T obj, int bitIndex) where T : unmanaged
    {
        switch (obj)
        {
            case sbyte sb:
                return (sb & (sbyte)(1 << bitIndex)) != 0;

            case byte b:
                return (b & (byte)(1 << bitIndex)) != 0;

            case short s:
                return (s & (short)(1 << bitIndex)) != 0;

            case ushort us:
                return (us & (ushort)(1 << bitIndex)) != 0;

            case int i:
                return (i & (1 << bitIndex)) != 0;

            case uint ui:
                return (ui & (1u << bitIndex)) != 0;

            case long l:
                return (l & (1L << bitIndex)) != 0;

            case ulong ul:
                return (ul & (1ul << bitIndex)) != 0;

            default:
                dynamic dynamicObj = obj;
                return (dynamicObj & (dynamic)(1 << bitIndex)) != 0;
                //throw new NotSupportedException($"Type {typeof(T)} is not supported for bit operations.");
        }
    }
}