using BW = System.IO.BinaryWriter;
using BR = System.IO.BinaryReader;
using MS = System.IO.MemoryStream;

namespace Cutulu
{
    public static class Buffer
    {
        #region Sending         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Packages your values in byte array with extra information for your generic transport protocol  
        /// </summary>
        public static byte[] PackageRaw<T>(this T value, ushort key, Method method)
        {
            // If is null
            if (value == null)
            {
                return null;
            }

            // Convert to bytes
            byte[] bytes = value.Buffer();

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

            // Write key
            wrtr.Write(key);

            // Write custom values
            wrtr.Write(bytes);

            // Close streams
            strm.Close();
            wrtr.Close();

            return strm.ToArray();
        }

        /// <summary> 
        /// Packages your values in byte array with extra information for your UDP transport protocol  
        /// </summary>
        public static byte[] PackageRawUdpClient<T>(this T value, ushort key, ushort udpSafety)
        {
            // If is null
            if (value == null)
            {
                return null;
            }

            // Convert to bytes
            byte[] bytes = value.Buffer();

            // Establish streams
            using MS strm = new();
            using BW wrtr = new(strm);

            // Write constant data
            wrtr.Write(udpSafety);
            wrtr.Write(key);

            // Write custom values
            wrtr.Write(bytes);

            // Close streams
            strm.Close();
            wrtr.Close();

            return strm.ToArray();
        }
        #endregion

        #region Receiving       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Unpackages your value from a prepared byte array  
        /// </summary>
        public static bool Unpack<T>(this byte[] bytes, out T value)
        => bytes.TryBuffer(out value);

        /// <summary> 
        /// Unpackages your value from a prepared byte array  
        /// </summary>
        public static T Unpack<T>(this byte[] bytes)
        => bytes.Buffer<T>();

        /// <summary> 
        /// Unpackages your values from byte array with extra information of your generic transport protocol  
        /// </summary>
        public static byte[] UnpackRaw(this byte[] buffer, out byte key)
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

        /// <summary> 
        /// Unpackages your values from byte array with extra information of your udp transport protocol  
        /// </summary>
        public static byte[] UnpackRawUdpServer(this byte[] buffer, out byte key, out ushort udpSafety)
        {
            // Buffer could not be read
            if (buffer == null || buffer.Length < 1)
            {
                udpSafety = default;
                key = 0;

                return default;
            }

            // Establish streams
            using MS strm = new(buffer);
            using BR rdr = new(strm);

            // Udp safety
            udpSafety = rdr.ReadUInt16();

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

    /// <summary> 
    /// Define your methods here. Tcp and Udp are already contained
    /// </summary>
    public enum Method : byte
    {
        Tcp, Udp
    }

    public struct NetworkPackage
    {
        public byte[] Content { get; private set; }
        public Method Method { get; private set; }
        public short Key { get; private set; }

        public NetworkPackage(short key, byte[] content, Method method)
        {
            Content = content;
            Method = method;
            Key = key;
        }

        public readonly bool TryBuffer<T>(out T value) => Content.TryBuffer(out value);
    }
}