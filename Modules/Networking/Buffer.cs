using BW = System.IO.BinaryWriter;
using BR = System.IO.BinaryReader;
using MS = System.IO.MemoryStream;

using System;

namespace Cutulu
{
    public static class Buffer
    {
        #region Sending
        /// <summary> assignLength is for udp packets </summary>
        /// <returns> Buffer with byte[] length, type, key and then the bytes </returns>
        public static byte[] Package<T>(this T value, byte key, Method method)
        {
            // Convert to bytes
            byte[] bytes = value.SerializeValue();

            // Prepare buffer
            byte[] buffer = new byte[bytes.Length + (byte)method];

            // Establish streams
            using MS strm = new(buffer);
            using BW wrtr = new(strm);

            // Write constant data
            if (method == Method.Tcp) wrtr.Write((ushort)bytes.Length);
            wrtr.Write(key);

            // Write custom values
            Array.Copy(bytes, 0, buffer, (byte)method, bytes.Length);

            // Close streams
            strm.Close();
            wrtr.Close();

            return buffer;
        }
        #endregion

        #region Receiving
        /// <summary> hasLength is for udp packets </summary>
        /// <returns> Length, type, key and bytes, transmitted by buffer </returns>
        public static T Unpack<T>(this byte[] buffer, out byte key, Method method)
        {
            // Buffer could not be read
            if (buffer == null || buffer.Length < (byte)method)
            {
                key = 0;

                return default;
            }

            // Establish streams
            using MS strm = new(buffer);
            using BR rdr = new(strm);

            // Read constant data
            ushort length;
            if (method == Method.Tcp) length = rdr.ReadUInt16();
            else length = (ushort)(buffer.Length - 1);
            key = rdr.ReadByte();

            // Read custom values
            T value = rdr.ReadBytes(length).DeserializeValue<T>();

            // Close streams
            strm.Close();
            rdr.Close();

            return value;
        }

        /// <summary> hasLength is for udp packets </summary>
        /// <returns> Length, type, key and bytes, transmitted by buffer </returns>
        public static byte[] Unpack(this byte[] buffer, out byte key, Method method)
        {
            // Buffer could not be read
            if (buffer == null || buffer.Length < (byte)method)
            {
                key = 0;

                return default;
            }

            // Establish streams
            using MS strm = new(buffer);
            using BR rdr = new(strm);

            // Read constant data
            ushort length;
            if (method == Method.Tcp) length = rdr.ReadUInt16();
            else length = (ushort)(buffer.Length - 1);
            key = rdr.ReadByte();

            // Read custom values
            byte[] bytes = rdr.ReadBytes(length);

            // Close streams
            strm.Close();
            rdr.Close();

            return bytes;
        }
        #endregion
    }

    public enum Method
    {
        Tcp = 3, Udp = 1
    }
}