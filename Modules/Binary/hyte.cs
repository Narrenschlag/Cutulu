namespace Cutulu
{
    /// <summary>
    /// Half byte.
    /// </summary>
    public struct hyte
    {
        public byte Byte { get; private set; }

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

        public void SetFirstNibble(byte value)
        {
            if (value > 0x0F)
                throw new("Value must be between 0x00 and 0x0F (inclusive).");

            Byte = (byte)((value << 4) | (Byte & 0x0F));
        }

        public void SetSecondNibble(byte value)
        {
            if (value > 0x0F)
                throw new("Value must be between 0x00 and 0x0F (inclusive).");

            Byte = (byte)((Byte & 0xF0) | value);
        }

        public override string ToString()
        {
            return $"{{ 0x{GetFirst():X}, 0x{GetSecond():X} }}";
        }
    }
}