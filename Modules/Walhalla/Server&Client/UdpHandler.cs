using System.Threading.Tasks;
using System.Net.Sockets;

namespace Walhalla
{
    public class UdpHandler
    {
        public UdpClient client;

        public delegate void UdpPacket(BufferType type, byte key, byte[]? bytes);
        public UdpPacket? onReceive;

        /// <summary> Creates handle on server side </summary>
        public UdpHandler(int port, UdpPacket onReceive)
        {
            client = new UdpClient(port);
            this.onReceive = onReceive;

            _listen();
        }

        /// <summary> Creates handle on client side </summary>
        public UdpHandler(string host, int port, UdpPacket onReceive)
        {
            client = new UdpClient();

            client.Connect(host, port);

            this.onReceive = onReceive;

            _listen();
        }

        /// <summary> Closes local network elements </summary>
        public void close()
        {
            if (client != null) client.Close();

            onReceive = null;
        }

        #region Send Data
        /// <summary> Sends data through connection </summary>
        public void send<T>(byte key, T? value)
        {
            client.Send(value.encodeBytes(key));
        }

        /// <summary> Sends data through connection </summary>
        public void send(BufferType type, byte key, byte[]? bytes)
        {
            if (bytes == null) bytes = new byte[0];

            client.Send(bytes.encodeBytes(type, key));
        }
        #endregion

        #region Receive Data
        private async void _listen()
        {
            while (true)
            {
                try { await _receive(); }
                catch { }
            }
        }

        private async Task _receive()
        {
            // Read length
            UdpReceiveResult result = await client.ReceiveAsync();
            byte[] buffer = result.Buffer;

            byte[]? bytes = Bufferf.decodeBytes(buffer, out int length, out BufferType type, out byte key);
            if (onReceive != null && type != BufferType.None) onReceive(type, key, bytes);
        }
        #endregion
    }
}