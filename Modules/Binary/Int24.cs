namespace Cutulu
{
    /// <summary>
    /// A 3 byte signed int. [-8388608 - 8388607]
    /// </summary>
    public struct Int24
    {
        public const int MinValue = -8388608; // -2^23
        public const int MaxValue = 8388607;  // (2^23 - 1)

        public byte[] Bytes;

        public Int24() => Bytes = null;
        public Int24(int value)
        {
            if (value < MinValue || value > MaxValue)
                throw new($"Value must be between {MinValue} and {MaxValue}.");

            Bytes = new byte[3];
            Bytes[0] = (byte)((value >> 16) & 0xFF);
            Bytes[1] = (byte)((value >> 8) & 0xFF);
            Bytes[2] = (byte)(value & 0xFF);
        }

        public override readonly string ToString() => ToInt32().ToString();
        public readonly int ToInt32()
        {
            // Ensure sign extension for negative numbers
            int result = (Bytes[0] << 16) | (Bytes[1] << 8) | Bytes[2];
            if ((result & 0x00800000) != 0)
                result |= unchecked((int)0xFF000000);
            return result;
        }

        public static implicit operator Int24(int value) => new(value);
        public static explicit operator int(Int24 value) => value.ToInt32();

        public static Int24 operator +(Int24 left, Int24 right) => new(left.ToInt32() + right.ToInt32());
        public static Int24 operator -(Int24 left, Int24 right) => new(left.ToInt32() - right.ToInt32());
    }

    /// <summary>
    /// A 3 byte unsigned int. [0 - 16777215]
    /// </summary>
    public struct UInt24
    {
        public const int MaxValue = 16777215;  // (2^24 - 1)
        public const int MinValue = 0;  // (0)

        public byte[] Bytes;

        public UInt24() => Bytes = null;
        public UInt24(uint value)
        {
            if (value > MaxValue)
                throw new($"Value must be between 0 and {MaxValue}.");

            Bytes = new byte[3];
            Bytes[0] = (byte)((value >> 16) & 0xFF);
            Bytes[1] = (byte)((value >> 8) & 0xFF);
            Bytes[2] = (byte)(value & 0xFF);
        }

        public override readonly string ToString() => ToUInt32().ToString();
        public readonly uint ToUInt32() => (uint)((Bytes[0] << 16) | (Bytes[1] << 8) | Bytes[2]);

        public static implicit operator UInt24(uint value) => new(value);
        public static explicit operator uint(UInt24 value) => value.ToUInt32();

        public static UInt24 operator +(UInt24 left, UInt24 right) => new(left.ToUInt32() + right.ToUInt32());
        public static UInt24 operator -(UInt24 left, UInt24 right) => new(left.ToUInt32() - right.ToUInt32());
    }
}