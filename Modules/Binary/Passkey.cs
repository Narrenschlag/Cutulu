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

        /// <summary>
        /// Contains key information
        /// </summary>
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

        /// <summary>
        /// Loads passkey from byte array
        /// </summary>
        public Passkey(byte[] bytes) => Key = bytes;

        /// <summary>
        /// Writes passkey to path
        /// </summary>
        public void Write(string path) => IO.Write(Key, path, IO.FileType.Binary);

        /// <summary>
        /// Loads passkey from path
        /// </summary>
        public Passkey Read(string path) => IO.TryRead(path, out Passkey key, IO.FileType.Binary) ? key : default;

        /// <summary>
        /// Compare and validate key
        /// </summary>
        public bool Validate(ref Passkey key) => Equals(key);

        /// <summary>
        /// Compare and validate key
        /// </summary>
        public bool Validate(ref byte[] bytes) => Equals(new Passkey(bytes));
    }
}