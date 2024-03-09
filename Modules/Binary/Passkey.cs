namespace Cutulu
{
    public struct Passkey
    {
        public const byte Tiny = 8;
        public const byte Small = 16;
        public const byte Medium = 32;
        public const byte Big = 64;
        public const byte Giant = 128;
        public const ushort Safe = 256;

        public byte[] Key { get; private set; }

        /// <summary>
        /// Generates a 256 byte long pass key with 256²⁵⁶ possible outcomes
        /// </summary>
        public Passkey() : this(Safe) { }

        /// <summary>
        /// Generates a length byte long pass key with 256^length possible outcomes
        /// </summary>
        public Passkey(ushort length)
        {
            Key = new byte[length];

            for (ushort i = 0; i < length; i++)
            {
                Key[i] = (byte)Random.RangeInt(0, 256);
            }
        }
    }
}