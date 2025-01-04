namespace Cutulu.Core
{
    public struct Mask
    {
        public bool[] Bits;

        public bool this[int bitIndex]
        {
            get => Get(bitIndex);
            set => Set(bitIndex, value);
        }

        public Mask()
        {
            throw new("Cannot create empty mask. needs atleast one bit!");
        }

        public Mask(int bitCount, bool fill = false)
        {
            Bits = new bool[bitCount];

            if (fill)
                for (int i = 0; i < bitCount; i++)
                    Bits[i] = true;
        }

        public Mask(int integer, int bitCount) : this(bitCount)
        {
            for (int i = 0; i < bitCount; i++)
                Bits[i] = BitBuilder.GetBit(integer, i);
        }

        public readonly void Set(int bit, bool newValue) => Bits[bit] = newValue;
        public bool Get(int bit) => Bits[bit];

        public int Integer
        {
            get
            {
                int x = 0;

                for (int i = 0; i < Bits.Length; i++)
                    x = BitBuilder.SetBit(x, i, Get(i));

                return x;
            }
        }
    }
}