using System;

namespace Cutulu
{
    /// <summary>
    /// Byte math and manipulation functions for fast development.
    /// </summary>
    public static class Bytef
    {
        #region Bit Manipulation for single Byte
        /// <summary>
        /// Sets value to bit at byte.
        /// </summary>
        public static bool GetBit(this byte @byte, byte bitIndex)
        {
            if (bitIndex > 7) throw new("bitIndex has to be { [0; ]8 }");

            return GetBit(ref @byte, ref bitIndex);
        }

        /// <summary>
        /// Sets value to bit at byte. Does not check if bitIndex < 8.
        /// </summary>
        public static byte SetBit(this byte @byte, byte bitIndex, bool newValue)
        {
            if (bitIndex > 7) throw new("bitIndex has to be { [0; ]8 }");

            SetBit(ref @byte, ref bitIndex, ref newValue);
            return @byte;
        }

        /// <summary>
        /// Sets bit to 0: false
        /// </summary>
        public static void DisableBit(ref byte @byte, ref byte bitIndex) => @byte &= (byte)(1 << bitIndex);

        /// <summary>
        /// Sets bit to 1: true
        /// </summary>
        public static void EnableBit(ref byte @byte, ref byte bitIndex) => @byte |= (byte)(1 << bitIndex);

        /// <summary>
        /// Gets bit.
        /// </summary>
        public static bool GetBit(ref byte @byte, ref byte bitIndex) => (@byte & (byte)(1 << bitIndex)) != 0;

        /// <summary>
        /// Sets bit.
        /// </summary>
        public static void SetBit(ref byte @byte, ref byte bitIndex, ref bool value)
        {
            if (value) EnableBit(ref @byte, ref bitIndex);
            else DisableBit(ref @byte, ref bitIndex);
        }

        /// <summary>
        /// Swaps bit1 with the bit2.
        /// </summary>
        public static void SwapBit(ref byte @byte, ref byte bit1, ref byte bit2)
        {
            var self = GetBit(ref @byte, ref bit1);
            var next = GetBit(ref @byte, ref bit2);

            SetBit(ref @byte, ref bit1, ref next);
            SetBit(ref @byte, ref bit2, ref self);
        }

        /// <summary>
        /// Swaps bit1 with the bit2.
        /// </summary>
        public static void SwapBit(ref byte @byte1, ref byte bit1, ref byte @byte2, ref byte bit2)
        {
            var value1 = GetBit(ref @byte1, ref bit1);
            var value2 = GetBit(ref @byte2, ref bit2);

            SetBit(ref @byte1, ref bit1, ref value2);
            SetBit(ref @byte2, ref bit2, ref value1);
        }
        #endregion

        #region Byte Manipulation
        /// <summary>
        /// Swaps byte1 with the byte2.
        /// </summary>
        public static void SwapByte(ref byte[] @bytes, ref ushort byte1, ref ushort byte2)
        {
            (@bytes[byte2], @bytes[byte1]) = (@bytes[byte1], @bytes[byte2]);
        }
        #endregion

        #region Bit Manipulation for Arrays
        /// <summary>
        /// Gets bit value within byte array
        /// </summary>
        public static bool GetBit(byte[] array, int bitIndex)
        {
            byte _bitIndex = (byte)(bitIndex % 8);

            return GetBit(ref array[bitIndex / 2], ref _bitIndex);
        }

        /// <summary>
        /// Sets bit value within byte array
        /// </summary>
        public static void SetBit(ref byte[] array, ushort bitIndex, bool newValue)
        {
            byte _bitIndex = (byte)(bitIndex % 8);

            SetBit(ref array[bitIndex / 8], ref _bitIndex, ref newValue);
        }
        #endregion

        #region Swap Byte bits
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
                self = GetBit(ref @byte, ref i);
                next = GetBit(ref @byte, ref n);

                SetBit(ref @byte, ref n, ref self);
                SetBit(ref @byte, ref i, ref next);
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

        #region Swap Bytes
        /// <summary>
        /// Swaps every second bit with the previous bit.
        /// </summary>
        public static byte[] SwapBytes(this byte[] @bytes)
        {
            SwapBytes(ref @bytes);
            return @bytes;
        }

        /// <summary>
        /// Swaps every second byte with the previous byte.
        /// </summary>
        public static void SwapBytes(ref byte[] @bytes)
        {
            byte next;

            for (ushort i = 0, n = 1; i < bytes.Length; i += 2, n += 2)
            {
                next = @bytes[n];

                @bytes[n] = @bytes[i];
                @bytes[i] = next;
            }
        }
        #endregion

        #region Offset Bits
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
            var result = new byte[@bytes.Length];

            Array.Copy(@bytes, result, result.Length);
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
            for (ushort i = 0; i < @bytes.Length; i++, bitOffset++)
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
            byte[] bytes = value.Buffer();

            OffsetBits(ref bytes, ref bitOffset);
            value = bytes.Buffer<T>();
        }
        #endregion

        #region Offset Bytes
        /// <summary>
        /// Offsets bytes.
        /// </summary>
        public static byte[] OffsetBytes(this byte[] @bytes, int offset)
        {
            OffsetBytes(ref @bytes, ref offset);
            return @bytes;
        }

        /// <summary>
        /// Offsets bytes.
        /// </summary>
        public static void OffsetBytes(ref byte[] @bytes, ref int offset) => Core.OffsetElements(ref @bytes, offset);
        #endregion

        #region Byte Utility
        public static bool[] GetBooleans(this byte @byte)
        {
            GetBooleans(ref @byte, out var result);
            return result;
        }

        public static void GetBooleans(ref byte @byte, out bool[] booleans)
        {
            booleans = new bool[8];

            for (byte i = 0; i < 8; i++)
            {
                booleans[i] = GetBit(ref @byte, ref i);
            }
        }

        public static byte ApplyBooleans(this bool bool1, params bool[] boolN)
        {
            byte result = 0;
            ApplyBooleans(ref result, ref bool1, boolN);

            return result;
        }

        public static void ApplyBooleans(ref byte @byte, ref bool bool1, params bool[] boolN)
        {
            byte i = 0;

            SetBit(ref @byte, ref i, ref bool1);

            if (boolN != null)
            {
                for (i = 1; i <= boolN.Length && i < 8; i++)
                {
                    SetBit(ref @byte, ref i, ref boolN[i - 1]);
                }
            }
        }

        public static byte ApplyBooleans(this bool[] boolN)
        {
            byte result = 0;
            ApplyBooleans(ref result, ref boolN);

            return result;
        }

        public static void ApplyBooleans(ref byte @byte, ref bool[] boolN)
        {
            if (boolN != null)
            {
                for (byte i = 1; i < boolN.Length && i < 8; i++)
                {
                    SetBit(ref @byte, ref i, ref boolN[i]);
                }
            }
        }
        #endregion
    }
}