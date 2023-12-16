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

            typeId = getTypeId<T>();
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
                    case BufferType.ByteArray: return (byte[])obj;

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

        /// <summary> Returns byte data as type </summary>
        public static T As<T>(this byte[] bytes) => fromBytes<T>(bytes);

        /// <summary> Returns byte data as type </summary>
        public static T fromBytes<T>(this byte[] bytes)
        {
            object obj = fromBytes(bytes, getTypeId<T>(), out bool json);

            if (obj != null && json && typeof(T) != typeof(string))
                return (obj as string).json<T>();

            return (T)obj;
        }

        public static object fromBytes(this byte[] bytes, BufferType type, out bool mayBeJson)
        {
            if (bytes == null)
            {
                "!!! WARNING !!!\nWas unable to fetch data from package due to it being emtpy".Log();
                mayBeJson = false;
                return default;
            }

            mayBeJson = type == BufferType.String;
            switch (type)
            {
                case BufferType.Boolean: return BitConverter.ToBoolean(bytes);

                case BufferType.Byte: return bytes[0];
                case BufferType.ByteArray: return bytes;

                case BufferType.Short: return BitConverter.ToInt16(bytes);
                case BufferType.UnsignedShort: return BitConverter.ToUInt16(bytes);

                case BufferType.Integer: return BitConverter.ToInt32(bytes);
                case BufferType.UnsignedInteger: return BitConverter.ToUInt32(bytes);

                case BufferType.Float: return BitConverter.ToSingle(bytes);
                case BufferType.Double: return BitConverter.ToDouble(bytes);

                case BufferType.String: return Enc.UTF8.GetString(bytes);
                case BufferType.Char: return BitConverter.ToChar(bytes);

                default: return default;
            }
        }

        public static BufferType getTypeId<T>()
        {
            Type t = typeof(T);

            // Boolean + Byte
            if (t == typeof(bool)) return BufferType.Boolean;

            else if (t == typeof(byte)) return BufferType.Byte;
            else if (t == typeof(byte[])) return BufferType.ByteArray;

            // Short
            if (t == typeof(short)) return BufferType.Short;
            else if (t == typeof(ushort)) return BufferType.UnsignedShort;

            // Integer
            if (t == typeof(int)) return BufferType.Integer;
            else if (t == typeof(uint)) return BufferType.UnsignedInteger;

            // Float
            else if (t == typeof(float)) return BufferType.Float;

            // Double
            else if (t == typeof(double)) return BufferType.Double;

            // String + Char
            else if (t == typeof(string)) return BufferType.String;
            else if (t == typeof(char)) return BufferType.Char;

            return BufferType.None;
        }

        public static Type getType(this BufferType type)
        {
            // Boolean + Byte
            switch (type)
            {
                case BufferType.Boolean: return typeof(bool);

                case BufferType.Byte: return typeof(byte);
                case BufferType.ByteArray: return typeof(byte[]);

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
        /// <summary> assignLength is for udp packets </summary>
        /// <returns> Buffer with byte[] length, type, key and then the bytes </returns>
        public static byte[] encodeBytes<T>(this T value, byte key, bool assignLength = true) => encodeBytes(value.toBytes(out BufferType typeId), typeId, key, assignLength);

        /// <summary> assignLength is for udp packets </summary>
        /// <returns> Buffer with byte[] length, type, key and then the bytes </returns>
        public static byte[] encodeBytes(this byte[] bytes, BufferType type, byte key, bool assignLength = true)
        {
            byte[] buffer = new byte[bytes.Length + (assignLength ? 6 : 2)];

            // Writes length of array into size array
            using (MemoryStream mem = new MemoryStream(buffer))
            {
                using (BinaryWriter BW = new BinaryWriter(mem))
                {
                    if (assignLength) BW.Write(bytes.Length);
                    BW.Write((byte)type);
                    BW.Write(key);
                }
            }

            Array.Copy(bytes, 0, buffer, assignLength ? 6 : 2, bytes.Length);
            return buffer;
        }
        #endregion

        #region Receiving
        /// <summary> hasLength is for udp packets </summary>
        /// <returns> Length, type, key and bytes, transmitted by buffer </returns>
        public static byte[] decodeBytes(this byte[] buffer, out int length, out BufferType type, out byte key, bool hasLength = true)
        {
            if (buffer == null || buffer.Length < (hasLength ? 6 : 2))
            {
                type = BufferType.None;
                length = 0;
                key = 0;

                return null;
            }

            byte[] bytes = new byte[buffer.Length - (hasLength ? 6 : 2)];
            using (MemoryStream mem = new MemoryStream(buffer))
            {
                using (BinaryReader BR = new BinaryReader(mem))
                {
                    if (hasLength) length = BR.ReadInt32();
                    else length = buffer.Length - 2;

                    type = (BufferType)BR.ReadByte();
                    key = BR.ReadByte();
                }
            }

            Array.Copy(buffer, hasLength ? 6 : 2, bytes, 0, length);
            return bytes;
        }
        #endregion

        #region Experimental Dot
        /// <summary> Uses the first four bits for unsigned Int8s </summary>
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

        /// <summary> Uses the first four bits for unsigned Int8s </summary>
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

        Boolean, Byte, ByteArray,
        Short, UnsignedShort,
        Integer, UnsignedInteger,
        Float, Double,
        String, Char
    }
}