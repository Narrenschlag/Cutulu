using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System;

namespace Cutulu
{
    public class TcpProtocol : Protocol
    {
        public const short WelcomeKey = 0;

        public NetworkStream stream;
        public TcpClient client;

        public Empty onDisconnect;
        public Packet onReceive;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Creates handle on server side 
        /// </summary>
        public TcpProtocol(ref TcpClient client, uint welcome, Packet onReceive, Empty onDisconnect, byte receiveTimeout = 5) : base(0)
        {
            client.ReceiveTimeout = 6000 * receiveTimeout;
            this.onDisconnect = onDisconnect;
            this.onReceive = onReceive;
            this.client = client;

            // Cancel setup if connection failed
            if ((Connected = client != null && client.Connected) == false)
            {
                Close();
                return;
            }

            stream = client.GetStream();

            short welcomeKey = WelcomeKey;
            Send(ref welcomeKey, welcome);

            Listen();
        }

        /// <summary> 
        /// Creates handle on client side 
        /// </summary>
        public TcpProtocol(string host, int port, Packet onReceive, Empty onDisconnect) : base(port)
        {
            this.onDisconnect = onDisconnect;
            this.onReceive = onReceive;
            client = new TcpClient();

            client.Connect(host, port);

            // Cancel setup if connection failed
            if ((Connected = client != null && client.Connected) == false)
            {
                Close();
                return;
            }

            stream = client.GetStream();

            Listen();
        }
        #endregion

        #region Send Data       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Sends data through connection 
        /// </summary>
        public void Send<T>(ref short key, T value)
        {
            ValidateConnection();

            byte[] bytes = Package(ref key, ref value);

            client.NoDelay = bytes.Length <= 1400;

            // Write and send data
            stream.Write(bytes);
            stream.Flush();
        }
        #endregion

        #region Receive Data    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Receive packets as long as client is connected 
        /// </summary>
        protected async void Listen()
        {
            while (Connected)
            {
                try { await Receive(); }
                catch (Exception ex)
                {
                    ex.Message.Log();
                    break;
                }
            }

            Disconnected();
        }

        /// <summary> 
        /// Waits for bytes received, reads and then formats them 
        /// </summary>
        protected async Task Receive()
        {
            // Define buffer for storage
            byte[] bytes = new byte[2];

            // Read length
            int i = await stream.ReadAsync(bytes.AsMemory(0, 2));

            // Catches disconnect StackOverflow
            if (i < 2) throw new IOException("connection corrupted");

            // Read length of buffer
            using MemoryStream mem = new(bytes);
            using BinaryReader BR = new(mem);
            int length = BR.ReadUInt16() + 2;
            mem.Close();
            BR.Close();

            // Read byte buffer
            bytes = new byte[length];
            await stream.ReadAsync(bytes.AsMemory(0, length));

            // Unpack bytes
            if (Unpack(bytes, out var package))
                onReceive?.Invoke(ref package);
        }
        #endregion

        #region End Connection  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Closes local network elements 
        /// </summary>
        public override void Close()
        {
            stream?.Close();
            client?.Close();

            onDisconnect = null;
            base.Close();
        }

        /// <summary>
        /// Called on client disconnect 
        /// </summary>
        private void Disconnected()
        {
            onDisconnect?.Invoke();

            Close();
        }
        #endregion

        #region Packaging
        public static byte[] Package<T>(ref short key, ref T value)
        {
            // Convert to bytes
            byte[] bytes = value == null ? Array.Empty<byte>() : value.Buffer();

            // Establish streams
            using MemoryStream strm = new();
            using BinaryWriter wrtr = new(strm);

            // Write constant data
            wrtr.Write((ushort)bytes.Length);

            // Write key
            wrtr.Write(key);

            // Write custom values
            wrtr.Write(bytes);

            // Close streams
            strm.Close();
            wrtr.Close();

            return strm.ToArray();
        }

        public static bool Unpack(byte[] buffer, out NetworkPackage package)
        {
            // Buffer could not be read
            if (buffer == null || buffer.Length < 2)
            {
                package = default;
                return false;
            }

            // Establish streams
            using MemoryStream strm = new(buffer);
            using BinaryReader rdr = new(strm);

            // Read key and contents
            package = new(rdr.ReadInt16(), rdr.ReadBytes(buffer.Length - 2), Method.Tcp);

            // Close streams
            strm.Close();
            rdr.Close();

            return true;
        }
        #endregion
    }
}