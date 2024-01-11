using BW = System.IO.BinaryWriter;
using BR = System.IO.BinaryReader;
using MS = System.IO.MemoryStream;

namespace Cutulu
{
    public static class Buffer
    {
        #region Sending
        /// <summary> assignLength is for udp packets </summary>
        /// <returns> Buffer with byte[] length, type, key and then the bytes </returns>
        public static byte[] Package<T>(this T value, byte key, Method method)
        {
            // If is null
            if (value == null)
            {
                return null;
            }

            // Convert to bytes
            byte[] bytes = value.Serialize();

            // Establish streams
            using MS strm = new();
            using BW wrtr = new(strm);

            // Write constant data
            switch (method)
            {
                case Method.Tcp:
                    wrtr.Write((ushort)bytes.Length);
                    break;

                default:
                    break;
            }
            wrtr.Write(key);

            // Write custom values
            wrtr.Write(bytes);

            // Close streams
            strm.Close();
            wrtr.Close();

            return strm.ToArray();
        }
        #endregion

        #region Receiving
        /// <summary> hasLength is for udp packets </summary>
        /// <returns> Length, type, key and bytes, transmitted by buffer </returns>
        public static bool Unpack<T>(this byte[] bytes, out T value)
        => bytes.TryDeserialize(out value);

        /// <summary> hasLength is for udp packets </summary>
        /// <returns> Length, type, key and bytes, transmitted by buffer </returns>
        public static byte[] Unpack(this byte[] buffer, out byte key)
        {
            // Buffer could not be read
            if (buffer == null || buffer.Length < 1)
            {
                key = 0;
                return default;
            }

            // Establish streams
            using MS strm = new(buffer);
            using BR rdr = new(strm);

            // Only key is required
            key = rdr.ReadByte();

            // Read custom values
            byte[] bytes = rdr.ReadBytes(buffer.Length - 1);

            // Close streams
            strm.Close();
            rdr.Close();

            return bytes;
        }
        #endregion
    }

    public enum Method
    {
        Tcp, Udp
    }
}