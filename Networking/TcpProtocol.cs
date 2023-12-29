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
        public TcpProtocol(ref TcpClient client, uint welcome, Packet onReceive, Empty onDisconnect, int receiveTimeout = 5) : base(0)
        {
            client.ReceiveTimeout = 6000 * receiveTimeout;
            this.onReceive = onReceive;

            stream = client.GetStream();
            this.client = client;

            this.onDisconnect = onDisconnect;
            send(0, welcome);
            _listen();
        }

        /// <summary> Creates handle on client side </summary>
        public TcpProtocol(string host, int port, Packet onReceive, Empty onDisconnect) : base(port)
        {
            this.onReceive = onReceive;
            client = new TcpClient();

            client.Connect(host, port);

            stream = client.GetStream();

            this.onDisconnect = onDisconnect;
            _listen();
        }

        /// <summary> Returns if the client is connected </summary>
        public override bool Connected() => client != null && client.Connected;

        /// <summary> Closes local network elements </summary>
        public override void Close()
        {
            if (stream != null) stream.Close();
            if (client != null) client.Close();

            onDisconnect = null;
            base.Close();
        }

        #region Send Data
        /// <summary> Sends data through connection </summary>
        public void send<T>(byte key, T value)
        {
            _validateConnection();

            byte[] bytes = value.package(key);

            client.NoDelay = bytes.Length <= 1400;
            stream.Write(bytes);

            Flush();
        }

        /// <summary> Sends written data to server </summary>
        public void Flush() => stream.Flush();
        #endregion

        #region Receive Data
        /// <summary> Receive packets as long as client is connected </summary>
        protected async void _listen()
        {
            while (Connected())
            {
                try { await _receive(); }
                catch (Exception ex)
                {
                    ex.Message.Log();
                    break;
                }
            }

            _disconnected();
        }

        /// <summary> Waits for bytes received, reads and then formats them </summary>
        protected async Task _receive()
        {
            // Define buffer for strorage
            byte[] buffer = new byte[4];

            // Read length
            int i = await stream.ReadAsync(buffer, 0, 4);

            // Catches disconnect StackOverflow
            if (i < 4) throw new IOException("connection corrupted");

            // Read length of buffer
            int length = 0;
            using (MemoryStream mem = new MemoryStream(buffer))
            {
                using BinaryReader BR = new BinaryReader(mem);
                length = BR.ReadInt32();
            }

            byte[] bytes = new byte[length += 2];
            await stream.ReadAsync(bytes, 0, length);
            Array.Resize(ref buffer, 4 + length);
            Array.Copy(bytes, 0, buffer, 4, length);

            BufferType type = Buffer.unpack(buffer, out bytes, out byte key);
            if (type != BufferType.None && onReceive != null) onReceive(key, type, bytes, Method.Tcp);
        }
        #endregion

        /// <summary> Called on client disconnect </summary>
        private void _disconnected()
        {
            if (onDisconnect != null)
                onDisconnect();

            Close();
        }
    }
}