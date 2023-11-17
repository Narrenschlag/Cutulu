namespace Cutulu
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
            "Cannot create empty mask. needs atleast one bit!".Throw();
            Bits = null;
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
                Bits[i] = integer.GetBitAt(i);
        }

        public bool Get(int bit) => Bits[bit];
        public void Set(int bit, bool newValue)
        {
            Bits[bit] = newValue;
        }

        public int Integer
        {
            get
            {
                int x = 0;

                for (int i = 0; i < Bits.Length; i++)
                    Core.SetBitAt(ref x, i, Get(i));

                return x;
            }
        }
    }
}