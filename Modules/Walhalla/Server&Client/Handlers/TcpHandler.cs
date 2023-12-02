using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System;
using Cutulu;

namespace Walhalla
{
    public class TcpHandler : HandlerBase
    {
        public NetworkStream stream;
        public TcpClient client;

        public Empty? onDisconnect;

        /// <summary> Creates handle on server side </summary>
        public TcpHandler(ref TcpClient client, uint welcome, Packet onReceive, Empty onDisconnect, int receiveTimeout = 5) : base(0, onReceive)
        {
            client.ReceiveTimeout = 6000 * receiveTimeout;

            stream = client.GetStream();
            this.client = client;

            this.onDisconnect = onDisconnect;
            send(0, welcome);
            _listen();
        }

        /// <summary> Creates handle on client side </summary>
        public TcpHandler(string host, int port, Packet onReceive, Empty onDisconnect) : base(port, onReceive)
        {
            client = new TcpClient();

            client.Connect(host, port);

            stream = client.GetStream();

            this.onDisconnect = onDisconnect;
            _listen();
        }

        /// <summary> Returns if the client is connected </summary>
        public override bool Connected => client != null && client.Connected;

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
        public override void send<T>(byte key, T value)
        {
            base.send(key, value);

            stream.Write(value.encodeBytes(key));
            Flush();
        }

        /// <summary> Sends data through connection </summary>
        public override void send(byte key, BufferType type, byte[] bytes)
        {
            base.send(key, type, bytes);

            if (bytes == null) bytes = new byte[0];

            stream.Write(bytes.encodeBytes(type, key));
            Flush();
        }

        /// <summary> Sends written data to server </summary>
        public void Flush() => stream.Flush();
        #endregion

        #region Receive Data
        protected async void _listen()
        {
            while (Connected)
            {
                try { await _receive(); }
                catch (Exception ex)
                {
                    ex.Message.Log();
                    break;
                }
            }

            _onDisconnect();
        }

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

            bytes = Bufferf.decodeBytes(buffer, out length, out BufferType type, out byte key);
            if (onReceive != null && type != BufferType.None) onReceive(type, key, bytes);
        }
        #endregion

        private void _onDisconnect()
        {
            if (onDisconnect != null)
                onDisconnect();

            Close();
        }
    }
}