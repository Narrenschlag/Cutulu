using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;

namespace Walhalla
{
    public class UdpHandler : HandlerBase
    {
        public delegate void UdpPacket(byte key, BufferType type, byte[] bytes, IPEndPoint source);
        public UdpPacket serverSideReceive;

        private bool isServerClient;
        public UdpClient client;

        /// <summary> Creates handle on server side </summary>
        public UdpHandler(int port, UdpPacket onReceive) : base(port, null)
        {
            serverSideReceive = onReceive;
            isServerClient = true;

            try
            {
                client = new UdpClient();

                // Set the UDP client to reuse the address and port (optional)
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Bind the UDP client to a specific port
                client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                // Listens to udp signals
                _listen();
            }
            catch (Exception ex)
            {
                throw new Exception($"Was not able to establish a udp connection:\n{ex.Message}");
            }

            _listen();
        }

        /// <summary> Creates handle on client side </summary>
        public UdpHandler(string host, int port, Packet onReceive) : base(port, onReceive)
        {
            serverSideReceive = null;
            isServerClient = false;

            client = new UdpClient();
            client.Connect(host, port); // Use the same port as the UDP listener and the same adress as tcp endpoint

            _listen();
        }

        public override bool Connected => client != null && client.Client.Connected;

        /// <summary> Closes local network elements </summary>
        public override void Close()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }

            base.Close();
        }

        #region Send Data
        /// <summary> Sends data through connection </summary>
        public override void send<T>(byte key, T value)
        {
            base.send(key, value);

            if (Connected && client != null)
                client.Send(value.encodeBytes(key));
        }

        /// <summary> Sends data through connection </summary>
        public override void send(byte key, BufferType type, byte[] bytes)
        {
            base.send(key, type, bytes);

            if (Connected && client != null)
            {
                if (bytes == null) bytes = new byte[0];
                client.Send(bytes.encodeBytes(type, key));
            }
        }
        #endregion

        #region Receive Data
        private async void _listen()
        {
            while (Connected)
            {
                try { await _receive(); }
                catch { break; }
            }
        }

        private async Task _receive()
        {
            if (client == null) return;

            // Read length
            UdpReceiveResult result = await client.ReceiveAsync();
            byte[] buffer = result.Buffer;

            byte[] bytes = Bufferf.decodeBytes(buffer, out int length, out BufferType type, out byte key);
            if (type == BufferType.None) return;

            if (isServerClient && serverSideReceive != null) serverSideReceive(key, type, bytes, result.RemoteEndPoint);
            else if (onReceive != null) onReceive(key, type, bytes);
        }
        #endregion
    }
}