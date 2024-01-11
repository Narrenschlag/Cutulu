using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;

namespace Cutulu
{
    public class UdpProtocol : Protocol
    {
        public delegate void UdpPacket(byte key, byte[] bytes, IPEndPoint source);

        public UdpPacket serverSideReceive;
        public Packet clientSideReceive;

        private bool isServerClient;
        public UdpClient client;

        /// <summary> Creates handle on server side </summary>
        public UdpProtocol(int port, UdpPacket onReceive_server) : base(port)
        {
            serverSideReceive = onReceive_server;
            clientSideReceive = null;
            isServerClient = true;

            try
            {
                client = new UdpClient(port);
                Connected = true;

                // Set the UDP client to reuse the address and port (optional)
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Listens to udp signals
                _listen();
            }
            catch (Exception ex)
            {
                Close();

                throw new Exception($"Was not able to establish a udp connection:\n{ex.Message}");
            }

            _listen();
        }

        /// <summary> Creates handle on client side </summary>
        public UdpProtocol(string host, int udpPort, Packet onReceive_client) : base(udpPort)
        {
            clientSideReceive = onReceive_client;
            serverSideReceive = null;
            isServerClient = false;

            try
            {
                client = new UdpClient();
                Connected = true;

                client.Connect(host, udpPort); // Use the same port as the UDP listener and the same adress as tcp endpoint

                // Listens to udp signals
                if (onReceive_client != null) _listen();
            }
            catch (Exception ex)
            {
                Close();

                throw new Exception($"Was not able to establish a udp connection:\n{ex.Message}");
            }
        }

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
        /// <summary> Sends data through connection towards special ipendpoint (Server Side) </summary>
        public void send<T>(byte key, T value, IPEndPoint destination)
        {
            _validateConnection();

            if (destination != null)
            {
                byte[] bytes = value.PackageRaw(key, Method.Udp);
                client.Send(bytes, bytes.Length, destination);
            }
        }

        /// <summary> Sends data through connection (Client Side) </summary>
        public void send<T>(byte key, T value)
        {
            _validateConnection();

            byte[] bytes = value.PackageRaw(key, Method.Udp);
            client.Send(bytes, bytes.Length);
        }
        #endregion

        #region Receive Data
        /// <summary> Receive packets as long as client is connected </summary>
        private async void _listen()
        {
            while (Connected)
                try
                {
                    await _receive();
                }
                catch (Exception ex)
                {
                    $"udp-error: {ex.Message}".Log();
                }
        }

        /// <summary> Waits for bytes received, reads and then formats them </summary>
        private async Task _receive()
        {
            if (client == null) return;

            // Read package
            UdpReceiveResult result = await client.ReceiveAsync();
            byte[] buffer = result.Buffer;

            // Decode bytes
            byte[] bytes = Buffer.UnpackRaw(buffer, out byte key);
            if (bytes == null) return;

            // Invoke callback
            if (isServerClient)
            {
                if (serverSideReceive != null)
                {
                    serverSideReceive(key, bytes, result.RemoteEndPoint);
                }
            }

            else
            {
                if (clientSideReceive != null)
                {
                    clientSideReceive(key, bytes, Method.Udp);
                }
            }
        }
        #endregion
    }
}