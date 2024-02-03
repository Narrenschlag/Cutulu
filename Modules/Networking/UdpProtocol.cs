using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;

namespace Cutulu
{
    public class UdpProtocol : Protocol
    {
        public delegate void UdpPacket(byte key, byte[] bytes, IPEndPoint source, ushort safetyId);

        public UdpPacket serverSideReceive;
        public Packet clientSideReceive;

        private readonly bool isServerClient;
        public UdpClient client;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Creates handle on server side 
        /// </summary>
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
                Listen();
            }
            catch (Exception ex)
            {
                Close();

                throw new Exception($"Was not able to establish a udp connection:\n{ex.Message}");
            }

            Listen();
        }

        /// <summary> 
        /// Creates handle on client side 
        /// </summary>
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
                if (onReceive_client != null) Listen();
            }
            catch (Exception ex)
            {
                Close();

                throw new Exception($"Was not able to establish a udp connection:\n{ex.Message}");
            }
        }
        #endregion

        #region Send Data       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Sends data through connection towards special ipendpoint (Server Side) 
        /// </summary>
        public void Send<T>(byte key, T value, IPEndPoint destination)
        {
            ValidateConnection();

            if (destination != null)
            {
                byte[] bytes = value.PackageRaw(key, Method.Udp);
                client.Send(bytes, bytes.Length, destination);
            }
        }

        /// <summary> 
        /// Sends data through connection (Client Side) 
        /// </summary>
        public void Send<T>(byte key, T value, ushort safetyId)
        {
            ValidateConnection();

            byte[] bytes = value.PackageRawUdpClient(key, safetyId);
            client.Send(bytes, bytes.Length);
        }
        #endregion

        #region Receive Data    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Receive packets as long as client is connected 
        /// </summary>
        private async void Listen()
        {
            while (Connected)
                try
                {
                    await Receive();
                }
                catch (Exception ex)
                {
                    $"udp-error: {ex.Message}".Log();
                }
        }

        /// <summary> 
        /// Waits for bytes received, reads and then formats them 
        /// </summary>
        private async Task Receive()
        {
            if (client == null) return;

            // Read package
            UdpReceiveResult result = await client.ReceiveAsync();
            byte[] buffer = result.Buffer;

            // Invoke callback
            if (isServerClient)
            {
                // Decode bytes
                byte[] bytes = Buffer.UnpackRawUdpServer(buffer, out byte key, out ushort safetyId);
                if (bytes == null) return;

                serverSideReceive?.Invoke(key, bytes, result.RemoteEndPoint, safetyId);
            }

            else
            {
                // Decode bytes
                byte[] bytes = Buffer.UnpackRaw(buffer, out byte key);
                if (bytes == null) return;

                clientSideReceive?.Invoke(key, bytes, Method.Udp);
            }
        }
        #endregion

        #region End Connection  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Closes local network elements 
        /// </summary>
        public override void Close()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }

            base.Close();
        }
        #endregion
    }
}