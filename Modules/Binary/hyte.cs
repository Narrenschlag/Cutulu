namespace Cutulu
{
    /// <summary>
    /// Half byte.
    /// </summary>
    public struct hyte
    {
        public byte Byte;

        public hyte() => Byte = 0;
        public hyte(byte value)
        {
            if (value > 0x0F)
                throw new("Value must be between 0x00 and 0x0F (inclusive).");

            Byte = value;
        }

        public readonly byte GetFirst()
        {
            return (byte)(Byte >> 4);
        }

        public readonly byte GetSecond()
        {
            return (byte)(Byte & 0x0F);
        }

        public hyte SetFirst(byte value)
        {
            if (value > 0x0F)
                throw new("Value must be between 0x00 and 0x0F (inclusive).");

            return new() { Byte = (byte)((value << 4) | (Byte & 0x0F)) };
        }

        public hyte SetSecond(byte value)
        {
            if (value > 0x0F)
                throw new("Value must be between 0x00 and 0x0F (inclusive).");

            return new() { Byte = (byte)((Byte & 0xF0) | value) };
        }

        public override readonly string ToString()
        {
            return $"{{ 0x{GetFirst():X}, 0x{GetSecond():X} }}";
        }
    }
}