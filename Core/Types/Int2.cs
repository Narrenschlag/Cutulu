namespace Cutulu.Core
{
    /// <summary>
    /// Byte sized qarter bytes. (4 qytes are 1 byte)
    /// </summary>
    public struct Int2
    {
        public byte Byte;

        public Int2() => Byte = 0;
        public Int2(byte value)
        {
            if (value > 15)
                throw new("Value must be between 0 and 15 (inclusive).");

            Byte = value;
        }

        public readonly byte GetValue(byte index)
        {
            if (index < 0 || index > 3)
                throw new("Index must be between 0 and 3 (inclusive).");

            return (byte)((Byte >> (index * 2)) & 0x03);
        }

        public Int2 SetValue(byte index, int value)
        {
            if (index < 0 || index > 3)
                throw new("Index must be between 0 and 3 (inclusive).");

            if (value < 0 || value > 3)
                throw new("Value must be between 0 and 3 (inclusive).");

            int shiftAmount = index * 2;
            int clearMask = ~(0x03 << shiftAmount);

            var b = Byte;
            b &= (byte)clearMask;
            b |= (byte)(value << shiftAmount);

            return new() { Byte = b };
        }

        public override string ToString()
        {
            return $"{{ {GetValue(0)}, {GetValue(1)}, {GetValue(2)}, {GetValue(3)} }}";
        }
    }
}