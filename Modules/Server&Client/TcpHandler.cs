using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System;

namespace Walhalla
{
    public class TcpHandler
    {
        public NetworkStream stream;
        public TcpClient client;

        public delegate void TcpPacket(BufferType type, byte key, byte[]? bytes);
        public TcpPacket? onReceive;

        public delegate void Empty();
        public Empty? onDisconnect;

        /// <summary> Creates handle on server side </summary>
        public TcpHandler(ref TcpClient client, uint welcome, TcpPacket onReceive, Empty onDisconnect, int receiveTimeout = 5)
        {
            client.ReceiveTimeout = 6000 * receiveTimeout;

            stream = client.GetStream();
            this.client = client;

            this.onDisconnect = onDisconnect;
            this.onReceive = onReceive;

            send(0, welcome);
            _listen();
        }

        /// <summary> Creates handle on client side </summary>
        public TcpHandler(string host, int port, TcpPacket onReceive, Empty onDisconnect)
        {
            client = new TcpClient();

            client.Connect(host, port);

            stream = client.GetStream();

            this.onDisconnect = onDisconnect;
            this.onReceive = onReceive;

            _listen();
        }

        /// <summary> Returns if the client is connected </summary>
        public bool Connected => client != null && client.Connected;

        /// <summary> Closes local network elements </summary>
        public void close()
        {
            if (stream != null) stream.Close();
            if (client != null) client.Close();

            onDisconnect = null;
            onReceive = null;
        }

        #region Send Data
        /// <summary> Sends data through connection </summary>
        public void send<T>(byte key, T? value, bool flush = true)
        {
            stream.Write(value.encodeBytes(key));
            if (flush) this.flush();
        }

        /// <summary> Sends data through connection </summary>
        public void send(BufferType type, byte key, byte[]? bytes, bool flush = true)
        {
            if (bytes == null) bytes = new byte[0];

            stream.Write(bytes.encodeBytes(type, key));
            if (flush) this.flush();
        }

        /// <summary> Sends written data to server </summary>
        public void flush() => stream.Flush();
        #endregion

        #region Receive Data
        private async void _listen()
        {
            while (Connected)
            {
                try { await _receive(); }
                catch { break; }
            }

            _onDisconnect();
        }

        private async Task _receive()
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

            byte[]? bytes = new byte[length += 2];
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

            close();
        }
    }
}