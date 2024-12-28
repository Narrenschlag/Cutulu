using System;
using Godot;

namespace Cutulu.Core
{
    /// <summary>
    /// Byte math and manipulation functions for fast development.
    /// </summary>
    public static class Bytef
    {
        #region Bit Manipulation            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Sets value to bit at number.
        /// </summary>
        public static bool GetBit(this int @int, byte bitIndex)
        {
            if (bitIndex > 32) throw new("bitIndex has to be { [0; ]32 }");

            return ((@int >> bitIndex) & 1) == 1;
        }

        /// <summary>
        /// Sets value to bit at byte.
        /// </summary>
        public static bool GetBit(this byte @byte, byte bitIndex)
        {
            if (bitIndex > 7) throw new("bitIndex has to be { [0; ]8 }");

            return Bitf.GetBit(ref @byte, ref bitIndex);
        }

        /// <summary>
        /// Sets value to bit at byte. Does not check if bitIndex < 8.
        /// </summary>
        public static byte SetBit(this byte @byte, byte bitIndex, bool newValue)
        {
            if (bitIndex > 7) throw new("bitIndex has to be { [0; ]8 }");

            Bitf.SetBit(ref @byte, ref bitIndex, ref newValue);
            return @byte;
        }

        /// <summary>
        /// Sets values to bits at byte. Does not check if bits is null.
        /// </summary>
        public static void SetBits(ref byte @byte, params bool[] bits)
        {
            for (byte i = 0; i < bits.Length && i < 8; i++)
            {
                Bitf.SetBit(ref @byte, ref i, ref bits[i]);
            }
        }

        /// <summary>
        /// Sets values to bits at byte. Does not check if bits is null.
        /// </summary>
        public static byte SetBits(byte @byte, params bool[] bits)
        {
            SetBits(ref @byte, bits);
            return @byte;
        }

        /// <summary>
        /// Sets values to bits at byte. Does not check if bits is null.
        /// </summary>
        public static byte GetByte(params bool[] bits)
        {
            byte @byte = 0;

            SetBits(ref @byte, bits);

            return @byte;
        }

        /// <summary>
        /// Swaps bit1 with the bit2.
        /// </summary>
        public static void SwapBit(ref byte @byte, ref byte bit1, ref byte bit2)
        {
            var self = Bitf.GetBit(ref @byte, ref bit1);
            var next = Bitf.GetBit(ref @byte, ref bit2);

            Bitf.SetBit(ref @byte, ref bit1, ref next);
            Bitf.SetBit(ref @byte, ref bit2, ref self);
        }

        /// <summary>
        /// Swaps bit1 with the bit2.
        /// </summary>
        public static void SwapBit(ref byte @byte1, ref byte bit1, ref byte @byte2, ref byte bit2)
        {
            var value1 = Bitf.GetBit(ref @byte1, ref bit1);
            var value2 = Bitf.GetBit(ref @byte2, ref bit2);

            Bitf.SetBit(ref @byte1, ref bit1, ref value2);
            Bitf.SetBit(ref @byte2, ref bit2, ref value1);
        }
        #endregion

        #region Byte Manipulation           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Swaps byte1 with the byte2.
        /// </summary>
        public static void SwapByte(ref byte[] @bytes, ref ushort byte1, ref ushort byte2)
        {
            (@bytes[byte2], @bytes[byte1]) = (@bytes[byte1], @bytes[byte2]);
        }
        #endregion

        #region Bit Manipulation for Arrays ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gets bit value within byte array
        /// </summary>
        public static bool GetBit(byte[] array, int bitIndex)
        {
            byte _bitIndex = (byte)(bitIndex % 8);

            return Bitf.GetBit(ref array[bitIndex / 2], ref _bitIndex);
        }

        /// <summary>
        /// Sets bit value within byte array
        /// </summary>
        public static void SetBit(ref byte[] array, int bitIndex, bool newValue)
        {
            byte _bitIndex = (byte)(bitIndex % 8);

            Bitf.SetBit(ref array[bitIndex / 8], ref _bitIndex, ref newValue);
        }
        #endregion

        #region Swap Bits                   ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Swaps every second bit with the previous bit.
        /// </summary>
        public static byte SwapBits(this byte @byte)
        {
            SwapBits(ref @byte);
            return @byte;
        }

        /// <summary>
        /// Swaps every second bit with the previous bit.
        /// </summary>
        public static void SwapBits(ref byte @byte)
        {
            bool self, next;

            for (byte i = 0, n = 1; i < 8; i += 2, n += 2)
            {
                self = Bitf.GetBit(ref @byte, ref i);
                next = Bitf.GetBit(ref @byte, ref n);

                Bitf.SetBit(ref @byte, ref n, ref self);
                Bitf.SetBit(ref @byte, ref i, ref next);
            }
        }

        /// <summary>
        /// Offsets bytes and then swaps ever second byte with the previous byte. Then swap bits of the contained bytes
        /// </summary>
        public static byte[] SwapBits(this byte[] @bytes, int offset = 0)
        {
            SwapBits(ref @bytes, ref offset);
            return @bytes;
        }

        /// <summary>
        /// Offsets bytes and then swaps ever second byte with the previous byte. Then swap bits of the contained bytes
        /// </summary>
        public static void SwapBits(ref byte[] @bytes, ref int offset)
        {
            OffsetBytes(ref @bytes, ref offset);
            SwapBytes(ref @bytes);

            for (ushort i = 0; i < @bytes.Length; i++)
            {
                SwapBits(ref @bytes[i]);
            }
        }
        #endregion

        #region Swap Bytes                  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Swaps every second bit with the previous bit.
        /// </summary>
        public static byte[] SwapBytes(this byte[] @bytes)
        {
            byte[] result = @bytes.Duplicate();

            SwapBytes(ref result);

            return result;
        }

        /// <summary>
        /// Swaps every second byte with the previous byte.
        /// </summary>
        public static void SwapBytes(ref byte[] @bytes)
        {
            byte next;

            for (int i = 0, n = 1; i < bytes.Length; i += 2, n += 2)
            {
                next = @bytes[n];

                @bytes[n] = @bytes[i];
                @bytes[i] = next;
            }
        }
        #endregion

        #region Offset Bits                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Offsets bits.
        /// </summary>
        public static byte OffsetBits(this byte @byte, int offset)
        {
            OffsetBits(ref @byte, ref offset);
            return @byte;
        }

        /// <summary>
        /// Offsets bits.
        /// </summary>
        public static void OffsetBits(ref byte @byte, ref int offset)
        {
            // Ensure the offset is within the range of 0-7 (bits in a byte)
            offset %= 8;
            if (offset < 0) offset += 8;

            // Perform bitwise operations to shift the bits
            @byte = (byte)((@byte << offset) | (@byte >> (8 - offset)));
        }

        /// <summary>
        /// Offsets bytes' bits.
        /// </summary>
        public static byte[] OffsetBits(this byte[] @bytes, int bitOffset)
        {
            var result = @bytes.Duplicate();

            OffsetBits(ref result, ref bitOffset);
            return result;
        }

        /// <summary>
        /// Offsets bytes' bits.
        /// </summary>
        public static void OffsetBits(ref byte[] @bytes, ref int bitOffset)
        {
            int _byteOffset = bitOffset / 8;
            OffsetBytes(ref @bytes, ref _byteOffset);

            int _bitOffset = (byte)(bitOffset % 8);
            for (int i = 0; i < @bytes.Length; i++, bitOffset++)
            {
                bitOffset %= 8;

                OffsetBits(ref @bytes[i], ref _bitOffset);
            }
        }

        /// <summary>
        /// Offsets value bits.
        /// </summary>
        public static T OffsetBits<T>(this T value, int bitOffset)
        {
            OffsetBits(ref value, ref bitOffset);
            return value;
        }

        /// <summary>
        /// Offsets value bits.
        /// </summary>
        public static void OffsetBits<T>(ref T value, ref int bitOffset)
        {
            byte[] bytes = value.Encode();

            OffsetBits(ref bytes, ref bitOffset);
            value = bytes.Decode<T>();
        }
        #endregion

        #region Offset Bytes                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Offsets bytes.
        /// </summary>
        public static byte[] OffsetBytes(this byte[] @bytes, int offset)
        {
            var result = @bytes.Duplicate();

            OffsetBytes(ref result, ref offset);

            return result;
        }

        /// <summary>
        /// Offsets bytes.
        /// </summary>
        public static void OffsetBytes(ref byte[] @bytes, ref int offset) => ArrayExtension.OffsetElements(ref @bytes, offset);
        #endregion

        #region Byte Array Combination
        public static byte[] CombineByteArrays(params byte[][] arrays)
        {
            int totalLength = 0;
            foreach (byte[] array in arrays)
            {
                totalLength += array.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }
        #endregion

        public const byte EncodedCenterByte = 136;
        public const byte CenterByte = 128;

        public static byte EncodToByteMinus1(this float float1, float float2)
        {
            // Map float range (-1 to 1) to byte range (0 to 15)
            byte byte1 = (byte)(((float1 + 1f) * 7.5f) + 0.5f);
            byte byte2 = (byte)(((float2 + 1f) * 7.5f) + 0.5f);

            // Combine byte1 and byte2
            return (byte)((byte1 << 4) | byte2);
        }

        public static void DecodeFromByteMinus1(this byte encodedByte, out float float1, out float float2)
        {
            // Extract byte1 and byte2
            byte byte1 = (byte)(encodedByte >> 4);
            byte byte2 = (byte)(encodedByte & 0b00001111);

            // Map byte range (0 to 15) back to float range (-1 to 1)
            float1 = ((byte1 - 0.5f) / 7.5f) - 1f;
            float2 = ((byte2 - 0.5f) / 7.5f) - 1f;
        }

        public static byte[] ToBytes(this string hex)
        {
            if (hex.IsEmpty()) return null;

            hex = hex.Replace(" ", ""); // Remove any spaces

            if (hex.Length % 2 != 0)
                throw new("Hexadecimal string must have an even number of characters.");

            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = System.Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        public static string ToHex(this byte[] bytes)
        {
            if (bytes.IsEmpty()) return null;

            var hex = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                hex.AppendFormat("{0:X2}", b);
            }

            return hex.ToString();
        }

        /// <summary>
        /// Determines whether a byte array contains the specified sequence of bytes.
        /// </summary>
        /// <param name="caller">The byte array to be searched.</param>
        /// <param name="array">The byte to be found.</param>
        /// <returns>The first location of the sequence within the array, -1 if the sequence is not found.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static int Contains(this byte[] caller, byte[] array)
        {
            byte startValue, endValue;
            int result, arrayLength, searchBoundary, j, startLocation, endOffset;

            if (caller == null)
                throw new($"{nameof(caller)}");
            if (array == null)
                throw new($"{nameof(array)}");
            if (caller.Length == 0 || array.Length == 0)
                throw new($"Argument {(caller.Length == 0 ? nameof(caller) : nameof(array))} is empty.");

            if (array.Length > caller.Length)
                return -1;

            startValue = array[0];
            arrayLength = array.Length;

            if (arrayLength > 1)
            {
                result = -1;
                endValue = array[^1];
                endOffset = arrayLength - 1;
                searchBoundary = caller.Length - arrayLength;
                startLocation = -1;

                while ((startLocation = System.Array.IndexOf(caller, startValue, startLocation + 1)) >= 0)
                {
                    if (startLocation > searchBoundary)
                        break;

                    if (caller[startLocation + endOffset] == endValue)
                    {
                        for (j = 1; j < endOffset && caller[startLocation + j] == array[j]; j++) { }

                        if (j == endOffset)
                        {
                            result = startLocation;
                            break;
                        }
                    }
                }
            }
            else
            {
                result = System.Array.IndexOf(caller, startValue);
            }
            return result;
        }
    }
}