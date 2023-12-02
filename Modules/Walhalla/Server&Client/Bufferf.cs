using System.IO;
using System;

using Enc = System.Text.Encoding;
using Cutulu;
namespace Walhalla
{
    public static class Bufferf
    {
        #region Translation
        public static byte[] toBytes<T>(this T value, out BufferType typeId)
        {
            if (value == null)
            {
                typeId = BufferType.None;
                return @default();
            }

            typeId = getTypeId(value);
            object obj = value;

            if (typeId == BufferType.None)
            {
                typeId = BufferType.String;
                return Enc.UTF8.GetBytes(value.json());
            }

            else if (value != null)
                switch (typeId)
                {
                    case BufferType.Boolean: return BitConverter.GetBytes((bool)obj);
                    case BufferType.Byte: return new byte[1] { (byte)obj };

                    case BufferType.Short: return BitConverter.GetBytes((short)obj);
                    case BufferType.UnsignedShort: return BitConverter.GetBytes((ushort)obj);

                    case BufferType.Integer: return BitConverter.GetBytes((int)obj);
                    case BufferType.UnsignedInteger: return BitConverter.GetBytes((uint)obj);

                    case BufferType.Float: return BitConverter.GetBytes((float)obj);
                    case BufferType.Double: return BitConverter.GetBytes((double)obj);

                    case BufferType.String: return Enc.UTF8.GetBytes((string)obj);
                    case BufferType.Char: return BitConverter.GetBytes((char)obj);

                    default: return @default();
                }
            else return @default();

            byte[] @default() => new byte[0];
        }

        public static T fromBytes<T>(this byte[] bytes)
        {
            if (bytes == null)
            {
                "!!! WARNING !!!\nWas unable to fetch data from package due to it being emtpy".Log();
                return default(T);
            }

            switch (getTypeId(default(T)))
            {
                case BufferType.Boolean: return (T)(object)BitConverter.ToBoolean(bytes);
                case BufferType.Byte: return (T)(object)bytes[0];

                case BufferType.Short: return (T)(object)BitConverter.ToInt16(bytes);
                case BufferType.UnsignedShort: return (T)(object)BitConverter.ToUInt16(bytes);

                case BufferType.Integer: return (T)(object)BitConverter.ToInt32(bytes);
                case BufferType.UnsignedInteger: return (T)(object)BitConverter.ToUInt32(bytes);

                case BufferType.Float: return (T)(object)BitConverter.ToSingle(bytes);
                case BufferType.Double: return (T)(object)BitConverter.ToDouble(bytes);

                case BufferType.String: return (T)(object)Enc.UTF8.GetString(bytes);
                case BufferType.Char: return (T)(object)BitConverter.ToChar(bytes);

                default: return Enc.UTF8.GetString(bytes).json<T>();
            }
        }

        public static BufferType getTypeId<T>(this T value)
        {
            // Boolean + Byte
            if (value is bool) return BufferType.Boolean;
            else if (value is byte) return BufferType.Byte;

            // Short
            if (value is short) return BufferType.Short;
            else if (value is ushort) return BufferType.UnsignedShort;

            // Integer
            if (value is int) return BufferType.Integer;
            else if (value is uint) return BufferType.UnsignedInteger;

            // Float
            else if (value is float) return BufferType.Float;

            // Double
            else if (value is double) return BufferType.Double;

            // String + Char
            else if (value is string) return BufferType.String;
            else if (value is char) return BufferType.Char;

            return BufferType.None;
        }

        public static Type getType(this BufferType type)
        {
            // Boolean + Byte
            switch (type)
            {
                case BufferType.Boolean: return typeof(bool);
                case BufferType.Byte: return typeof(byte);

                case BufferType.Short: return typeof(short);
                case BufferType.UnsignedShort: return typeof(ushort);

                case BufferType.Integer: return typeof(int);
                case BufferType.UnsignedInteger: return typeof(uint);

                case BufferType.Float: return typeof(float);
                case BufferType.Double: return typeof(double);

                case BufferType.String: return typeof(string);
                case BufferType.Char: return typeof(char);

                default: return typeof(object);
            }
        }
        #endregion

        #region Sending
        /// <returns> Buffer with byte[] length, type, key and then the bytes </returns>
        public static byte[] encodeBytes<T>(this T value, byte key) => encodeBytes(value.toBytes(out BufferType typeId), typeId, key);

        /// <returns> Buffer with byte[] length, type, key and then the bytes </returns>
        public static byte[] encodeBytes(this byte[] bytes, BufferType type, byte key)
        {
            byte[] buffer = new byte[bytes.Length + 6];

            // Writes length of array into size array
            using (MemoryStream mem = new MemoryStream(buffer))
            {
                using (BinaryWriter BW = new BinaryWriter(mem))
                {
                    BW.Write(bytes.Length);
                    BW.Write((byte)type);
                    BW.Write(key);
                }
            }

            Array.Copy(bytes, 0, buffer, 6, bytes.Length);
            return buffer;
        }
        #endregion

        #region Receiving
        /// <returns> Length, type, key and bytes, transmitted by buffer
        public static byte[] decodeBytes(this byte[] buffer, out int length, out BufferType type, out byte key)
        {
            if (buffer == null || buffer.Length < 6)
            {
                type = BufferType.None;
                length = 0;
                key = 0;

                return null;
            }

            byte[] bytes = new byte[buffer.Length - 6];
            using (MemoryStream mem = new MemoryStream(buffer))
            {
                using (BinaryReader BR = new BinaryReader(mem))
                {
                    length = BR.ReadInt32();
                    type = (BufferType)BR.ReadByte();
                    key = BR.ReadByte();
                }
            }

            Array.Copy(buffer, 6, bytes, 0, length);
            return bytes;
        }
        #endregion

        #region Experimental Dot
        /// <summary>
        /// Uses the first four bits for unsigned Int8s
        /// </summary>
        public static byte dot(byte a, byte b)
        {
            if (a > 15) a = 15;
            if (b > 15) b = 15;
            int _ = 0;

            for (int i = 0; i < 8; i++)
            {
                Core.SetBitAt(ref _, i, i < 4 ? Core.GetBitAt(a, i) : Core.GetBitAt(b, i - 4));
            }

            return (byte)_;
        }

        /// <summary>
        /// Uses the first four bits for unsigned Int8s
        /// </summary>
        public static (byte, byte) dot(byte dot)
        {
            (int a, int b) _ = (0, 0);

            for (int i = 0; i < 8; i++)
            {
                if (i < 4) Core.SetBitAt(ref _.a, i, Core.GetBitAt(dot, i));
                else Core.SetBitAt(ref _.b, i - 4, Core.GetBitAt(dot, i));
            }

            return ((byte)_.a, (byte)_.b);
        }
        #endregion
    }

    public enum BufferType
    {
        None = 0,

        Boolean, Byte,
        Short, UnsignedShort,
        Integer, UnsignedInteger,
        Float, Double,
        String, Char
    }
}