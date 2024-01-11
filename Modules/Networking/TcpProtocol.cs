using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System;

namespace Cutulu
{
    public class TcpProtocol : Protocol
    {
        public NetworkStream stream;
        public TcpClient client;

        public Empty onDisconnect;
        public Packet onReceive;

        /// <summary> Creates handle on server side </summary>
        public TcpProtocol(ref TcpClient client, uint welcome, Packet onReceive, Empty onDisconnect, byte receiveTimeout = 5) : base(0)
        {
            client.ReceiveTimeout = 6000 * receiveTimeout;
            this.onReceive = onReceive;
            this.client = client;

            // Cancel setup if connection failed
            if ((Connected = client != null && client.Connected) == false)
            {
                Close();
                return;
            }

            stream = client.GetStream();

            this.onDisconnect = onDisconnect;
            Send(0, welcome);
            Listen();
        }

        /// <summary> Creates handle on client side </summary>
        public TcpProtocol(string host, int port, Packet onReceive, Empty onDisconnect) : base(port)
        {
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

            this.onDisconnect = onDisconnect;
            Listen();
        }

        /// <summary> Closes local network elements </summary>
        public override void Close()
        {
            stream?.Close();
            client?.Close();

            onDisconnect = null;
            base.Close();
        }

        #region Send Data
        /// <summary> Sends data through connection </summary>
        public void Send<T>(byte key, T value)
        {
            ValidateConnection();

            byte[] bytes = value.Package(key, Method.Tcp);

            client.NoDelay = bytes.Length <= 1400;
            stream.Write(bytes);

            Flush();
        }

        /// <summary> Sends written data to server </summary>
        public void Flush() => stream.Flush();
        #endregion

        #region Receive Data
        /// <summary> Receive packets as long as client is connected </summary>
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

        /// <summary> Waits for bytes received, reads and then formats them </summary>
        protected async Task Receive()
        {
            // Define buffer for strorage
            byte[] buffer = new byte[4];

            // Read length
            int i = await stream.ReadAsync(buffer.AsMemory(0, 4));

            // Catches disconnect StackOverflow
            if (i < 4) throw new IOException("connection corrupted");

            // Read length of buffer
            int length = 0;
            using (MemoryStream mem = new(buffer))
            {
                using BinaryReader BR = new(mem);
                length = BR.ReadInt32();
            }

            byte[] bytes = new byte[length += 2];
            await stream.ReadAsync(bytes.AsMemory(0, length));

            Array.Resize(ref buffer, 4 + length);
            Array.Copy(bytes, 0, buffer, 4, length);

            bytes = Buffer.Unpack(buffer, out byte key, Method.Tcp);
            if (bytes != null && onReceive != null) onReceive(key, bytes, Method.Tcp);
        }
        #endregion

        /// <summary> Called on client disconnect </summary>
        private void Disconnected()
        {
            onDisconnect?.Invoke();

            Close();
        }
    }
}