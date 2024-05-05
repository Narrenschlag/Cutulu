namespace Cutulu
{
    public static class Bitf
    {
        public static byte GetByte(params bool[] bits)
        {
            byte result = 0;

            for (byte i = 0; i < bits.Length && i < 8; i++)
            {
                if (bits[i]) EnableBit(ref result, ref i);
            }

            return result;
        }

        public static bool[] GetBits(ref byte value)
        {
            var result = new bool[8];

            for (byte i = 0; i < 8; i++)
            {
                result[i] = GetBit(ref value, ref i);
            }

            return result;
        }

        /// <summary>
        /// Sets bit to 0: false
        /// </summary>
        public static void DisableBit(ref byte @byte, ref byte bitIndex) => @byte &= (byte)(1 << bitIndex);

        /// <summary>
        /// Sets bit to 1: true
        /// </summary>
        public static void EnableBit(ref byte @byte, ref byte bitIndex) => @byte |= (byte)(1 << bitIndex);

        /// <summary>
        /// Gets bit.
        /// </summary>
        public static bool GetBit(ref byte @byte, ref byte bitIndex) => (@byte & (byte)(1 << bitIndex)) != 0;

        /// <summary>
        /// Sets bit.
        /// </summary>
        public static void SetBit(ref byte @byte, byte bitIndex, bool value) => SetBit(ref @byte, ref bitIndex, ref value);
        public static void SetBit(ref byte @byte, ref byte bitIndex, ref bool value)
        {
            if (value) EnableBit(ref @byte, ref bitIndex);
            else DisableBit(ref @byte, ref bitIndex);
        }
    }
}