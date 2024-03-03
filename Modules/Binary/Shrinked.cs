using System.IO;
using System;

namespace Cutulu
{
    public struct Shrinked
    {
        public byte[] Bytes;
        private Int2 Id;

        public Shrinked(int value1, int value2, int value3, int value4)
        {
            Id = new Int2();
            Bytes = null;

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            Set(ref value1, 0, ref writer);
            Set(ref value2, 1, ref writer);
            Set(ref value3, 2, ref writer);
            Set(ref value4, 3, ref writer);

            Bytes = stream.ToArray();
        }

        public Shrinked(byte[] buffer)
        {
            Id = new() { Byte = buffer[^1] };

            Bytes = new byte[buffer.Length - 1];
            Array.Copy(buffer, Bytes, Bytes.Length);
        }

        private void Set(ref int value, byte bit, ref BinaryWriter writer)
        {
            Id = Id.SetValue(bit, GetByteCount(ref value));
            switch (Id.GetValue(bit))
            {
                // sByte
                case 1:
                    writer.Write((byte)value);
                    break;

                // Short
                case 2:
                    writer.Write(((short)value).Buffer());
                    break;

                // Middle
                case 3:
                    writer.Write(((Int24)value).Bytes);
                    break;

                // Int
                default:
                    writer.Write(value.Buffer());
                    break;
            }
        }

        public object GetValue(byte index, out Type type)
        {
            byte offset = 0;

            for (byte i = 0; i < index; i++)
            {
                offset += GetByteCountOf(ref i);
            }

            var count = GetByteCountOf(ref index);
            count = count == 0 ? (byte)4 : count;
            var buffer = new byte[count];

            for (int i = 0; i < count; i++)
            {
                buffer[i] = Bytes[i + offset];
            }

            switch (count)
            {
                // sByte
                case 1:
                    type = typeof(sbyte);
                    return (sbyte)buffer[0];

                // Short
                case 2:
                    type = typeof(short);
                    return buffer.Buffer<short>();

                // Middle
                case 3:
                    type = typeof(Int24);
                    return new Int24() { Bytes = buffer };

                // Int
                default:
                    type = typeof(int);
                    return buffer.Buffer<int>();
            }
        }

        public byte[] GetBytesOf(byte index)
        {
            var obj = GetValue(index, out _);
            return GetByteCountOf(ref index) switch
            {
                // sByte
                1 => new byte[1] { (byte)obj },
                // Short
                2 => BitConverter.GetBytes((short)obj),
                // Middle
                3 => ((Int24)obj).Bytes,
                // Int
                _ => BitConverter.GetBytes((int)obj),
            };
        }

        private readonly byte GetByteCountOf(ref byte index) => Id.GetValue(index);
        private static byte GetByteCount(ref int value)
        {
            if (value >= sbyte.MinValue && value <= sbyte.MaxValue) return 1;        // sByte
            else if (value >= short.MinValue && value <= short.MaxValue) return 2;   // Short
            else if (value >= Int24.MinValue && value <= Int24.MaxValue) return 3;   // Middle

            return 0;   // Int
        }

        public readonly byte[] GetBuffer()
        {
            var result = new byte[Bytes.Length + 1];
            Array.Copy(Bytes, result, Bytes.Length);

            result[^1] = Id.Byte;
            return result;
        }
    }
}