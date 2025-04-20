namespace Cutulu.Network.Protocols
{
    using System.IO;

    using Core;

    public static class PacketProtocol
    {
        /// <summary>
        /// Packs data into a byte array.
        /// </summary>
        public static byte[] Pack(short key, object obj, out int length)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);

            writer.Write(key);
            writer.Write(obj.Encode());

            var buffer = memory.ToArray();
            length = buffer.Length;

            return buffer;
        }

        /// <summary>
        /// Unpacks data from a byte array.
        /// </summary>
        public static bool Unpack(byte[] buffer, out short Key, out byte[] Buffer)
        {
            if (buffer.Size() < 2)
            {
                Buffer = default;
                Key = default;

                return false;
            }

            using var stream = new MemoryStream(buffer);
            using var reader = new BinaryReader(stream);

            Key = reader.ReadInt16();
            Buffer = reader.ReadRemainingBytes();

            return true;
        }
    }
}