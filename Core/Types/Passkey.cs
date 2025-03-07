using System.Collections.Generic;

namespace Cutulu.Core
{
    public struct Passkey
    {
        public const string DefaultPath = $"{DefaultDirectory}LocalPasskey{FileEnding}";
        public const string DefaultDirectory = $"{IO.USER_PATH}.private/";
        public const string FileEnding = ".key";

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
        public Passkey(int length)
        {
            Key = new byte[length];

            for (ushort i = 0; i < length; i++)
            {
                Key[i] = (byte)Random.Range(0, 256);
            }
        }

        /// <summary>
        /// Loads passkey from byte array
        /// </summary>
        public Passkey(byte[] bytes) => Key = bytes;

        /// <summary>
        /// Writes passkey to path
        /// </summary>
        public readonly void Write(string path) => IO.Write(Key, path, IO.FileType.Binary);
        public readonly void WriteByName(string name) => Write($"{DefaultDirectory}{name}{FileEnding}");
        public readonly void Write() => Write(DefaultPath);

        /// <summary>
        /// Loads passkey from path
        /// </summary>
        public static Passkey Read(string path) => IO.TryRead(path, out Passkey key, IO.FileType.Binary) ? key : default;
        public static Passkey ReadByName(string name) => Read($"{DefaultDirectory}{name}{FileEnding}");
        public static Passkey Read() => Read(DefaultPath);

        public static KeyValuePair<string, Passkey>[] ReadAtDirectory(string path, string fileEnding = ".remote")
        {
            var paths = IO.GetFiles(path);

            var array = new KeyValuePair<string, Passkey>[paths.Size()];
            for (ushort i = 0; i < array.Length; i++)
            {
                array[i] = new($"{paths[i][..^fileEnding.Length]}", Read($"{path}{paths[i]}"));
            }

            return array;
        }

        /// <summary>
        /// Compare and validate key
        /// </summary>
        public bool Compare(ref Passkey key) => Equals(key);

        /// <summary>
        /// Compare and validate key
        /// </summary>
        public bool Compare(byte[] bytes) => Equals(new Passkey(bytes));
    }
}