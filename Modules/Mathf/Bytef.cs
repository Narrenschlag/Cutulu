using Godot;

namespace Cutulu
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
            byte[] bytes = value.Buffer();

            OffsetBits(ref bytes, ref bitOffset);
            value = bytes.Buffer<T>();
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
        public static void OffsetBytes(ref byte[] @bytes, ref int offset) => Core.OffsetElements(ref @bytes, offset);
        #endregion
    }
}