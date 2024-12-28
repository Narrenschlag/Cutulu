namespace Cutulu.Core
{
    public struct ByteContainer
    {
        public int PackedValue;

        public ByteContainer(params byte[] bytes)
        {
            PackedValue = PackBytes(bytes);
        }

        public int[] Unpack(byte numBytes) => UnpackBytes(PackedValue, numBytes);

        // Pack byte values into a single number
        static int PackBytes(params byte[] bytes)
        {
            int packedValue = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                packedValue |= bytes[i] << (i * 8);
            }
            return packedValue;
        }

        // Unpack a number into byte values
        static int[] UnpackBytes(int packedValue, int numBytes)
        {
            int[] unpackedValues = new int[numBytes];
            for (int i = 0; i < unpackedValues.Length; i++)
            {
                unpackedValues[i] = (packedValue >> (i * 8)) & 0xFF;
            }
            return unpackedValues;
        }
    }
}