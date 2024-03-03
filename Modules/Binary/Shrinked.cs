using System.Collections.Generic;

namespace Cutulu
{
    public struct Shrinked
    {
        private byte[] Bytes;
        private qyte Id;

        public Shrinked(int value1, int value2, int value3, int value4)
        {
            Id = new qyte();
            Bytes = null;

            List<byte> buffer = new();
            set(ref value1, 0, ref buffer);
            set(ref value2, 1, ref buffer);
            set(ref value3, 2, ref buffer);
            set(ref value4, 3, ref buffer);
            Bytes = buffer.ToArray();
        }

        private void set(ref int value, byte bit, ref List<byte> buffer)
        {
            Id = Id.SetValue(bit, getByteCount(ref value));

            switch (Id.GetValue(bit))
            {
                // Byte
                case 1:
                    buffer.Add((byte)value);
                    break;

                // Short
                case 2:
                    buffer.AddRange(((short)value).Buffer());
                    break;

                // Middle
                case 3:
                    buffer.AddRange(((middle)value).Bytes);
                    break;

                // Keep
                default:
                    buffer.AddRange(value.Buffer());
                    break;
            }
        }

        private static byte getByteCount(ref int value)
        {
            if (value >= sbyte.MinValue && value <= sbyte.MaxValue) return 1;        // Byte
            else if (value >= short.MinValue && value <= short.MaxValue) return 2;   // Short
            else if (value >= middle.MinValue && value <= middle.MaxValue) return 3; // Middle

            return 0;   // Int
        }
    }
}