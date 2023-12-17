using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;
using Cutulu;

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
                client = new UdpClient(port);

                // Set the UDP client to reuse the address and port (optional)
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

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
        public UdpHandler(string host, int udpPort, Delegates.Packet onReceive) : base(udpPort, onReceive)
        {
            serverSideReceive = null;
            isServerClient = false;

            try
            {
                client = new UdpClient();

                client.Connect(host, udpPort); // Use the same port as the UDP listener and the same adress as tcp endpoint

                // Listens to udp signals
                if (onReceive != null) _listen();
            }
            catch (Exception ex)
            {
                throw new Exception($"Was not able to establish a udp connection:\n{ex.Message}");
            }
        }

        public override bool Connected => client != null;

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
        public void send<T>(byte key, T value, IPEndPoint target)
        {
            if (Connected && client != null && target != null)
            {
                byte[] bytes = value.encodeBytes(key, false);
                client.Send(bytes, bytes.Length, target);
            }
        }

        /// <summary> Sends data through connection </summary>
        public override void send<T>(byte key, T value)
        {
            base.send(key, value);

            if (Connected && client != null)
            {
                byte[] bytes = value.encodeBytes(key, false);
                client.Send(bytes, bytes.Length);
            }
        }

        /// <summary> Sends data through connection </summary>
        public void send(byte key, BufferType type, byte[] bytes, IPEndPoint target)
        {
            if (Connected && client != null && target != null)
            {
                if (bytes == null) bytes = new byte[0];
                bytes = bytes.encodeBytes(type, key, false);

                client.Send(bytes, bytes.Length, target);
            }
        }

        /// <summary> Sends data through connection </summary>
        public override void send(byte key, BufferType type, byte[] bytes)
        {
            base.send(key, type, bytes);

            if (Connected && client != null)
            {
                if (bytes == null) bytes = new byte[0];
                bytes = bytes.encodeBytes(type, key, false);

                client.Send(bytes, bytes.Length);
            }
        }
        #endregion

        #region Receive Data
        /// <summary> Receive packets as long as client is connected </summary>
        private async void _listen()
        {
            while (Connected)
                try { await _receive(); }
                catch (Exception ex) { ("udp-error:" + ex.Message).Log(); }
        }

        /// <summary> Waits for bytes received, reads and then formats them </summary>
        private async Task _receive()
        {
            if (client == null) return;

            // Read package
            UdpReceiveResult result = await client.ReceiveAsync();
            byte[] buffer = result.Buffer;

            // Decode bytes
            byte[] bytes = Bufferf.decodeBytes(buffer, out int length, out BufferType type, out byte key, false);
            if (type == BufferType.None) return;

            // Invoke callback
            if (isServerClient && serverSideReceive != null) serverSideReceive(key, type, bytes, result.RemoteEndPoint);
            else if (onReceive != null) onReceive(key, type, bytes, Method.Udp);
        }
        #endregion
    }
}