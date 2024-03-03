namespace Cutulu
{
    /// <summary>
    /// A 3 byte signed int. [-8388608 - 8388607]
    /// </summary>
    public struct middle
    {
        public const int MinValue = -8388608; // -2^23
        public const int MaxValue = 8388607;  // (2^23 - 1)

        public byte[] Bytes;

        public middle() => Bytes = null;
        public middle(int value)
        {
            if (value < MinValue || value > MaxValue)
                throw new($"Value must be between {MinValue} and {MaxValue}.");

            Bytes = new byte[3];
            Bytes[0] = (byte)((value >> 16) & 0xFF);
            Bytes[1] = (byte)((value >> 8) & 0xFF);
            Bytes[2] = (byte)(value & 0xFF);
        }

        public int ToInt32()
        {
            // Ensure sign extension for negative numbers
            int result = (Bytes[0] << 16) | (Bytes[1] << 8) | Bytes[2];
            if ((result & 0x00800000) != 0)
                result |= unchecked((int)0xFF000000);
            return result;
        }

        public static implicit operator middle(int value)
        {
            return new middle(value);
        }

        public static explicit operator int(middle value)
        {
            return value.ToInt32();
        }

        public static middle operator +(middle left, middle right)
        {
            return new middle(left.ToInt32() + right.ToInt32());
        }

        public static middle operator -(middle left, middle right)
        {
            return new middle(left.ToInt32() - right.ToInt32());
        }
    }

    /// <summary>
    /// A 3 byte signed uint. [0 - 16777215]
    /// </summary>
    public struct umiddle
    {
        public const int MaxValue = 16777215;  // (2^24 - 1)
        public const int MinValue = 0;  // (0)

        public byte[] Bytes;

        public umiddle() => Bytes = null;
        public umiddle(uint value)
        {
            if (value > MaxValue)
                throw new($"Value must be between 0 and {MaxValue}.");

            Bytes = new byte[3];
            Bytes[0] = (byte)((value >> 16) & 0xFF);
            Bytes[1] = (byte)((value >> 8) & 0xFF);
            Bytes[2] = (byte)(value & 0xFF);
        }

        public readonly uint ToUInt32()
        {
            return (uint)((Bytes[0] << 16) | (Bytes[1] << 8) | Bytes[2]);
        }

        public static implicit operator umiddle(uint value)
        {
            return new umiddle(value);
        }

        public static explicit operator uint(umiddle value)
        {
            return value.ToUInt32();
        }

        public static umiddle operator +(umiddle left, umiddle right)
        {
            return new umiddle(left.ToUInt32() + right.ToUInt32());
        }

        public static umiddle operator -(umiddle left, umiddle right)
        {
            return new umiddle(left.ToUInt32() - right.ToUInt32());
        }
    }
}