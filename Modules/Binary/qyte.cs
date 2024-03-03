namespace Cutulu
{
    /// <summary>
    /// Quater byte.
    /// </summary>
    public struct qyte
    {
        public byte Byte { get; private set; }

        public qyte() => Byte = 0;
        public qyte(byte value)
        {
            if (value > 15)
                throw new("Value must be between 0 and 15 (inclusive).");

            Byte = value;
        }

        public int GetValue(int index)
        {
            if (index < 0 || index > 3)
                throw new("Index must be between 0 and 3 (inclusive).");

            return (Byte >> (index * 2)) & 0x03;
        }

        public qyte SetValue(int index, int value)
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