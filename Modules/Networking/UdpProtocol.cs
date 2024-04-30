using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;

namespace Cutulu
{
    public class UdpProtocol : Protocol
    {
        public delegate void UdpPacket(ref NetworkPackage package, IPEndPoint source, ushort safetyId);

        public UdpPacket serverSideReceive;
        public Packet clientSideReceive;

        private readonly bool isServerClient;
        public UdpClient client;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Creates handle on server side 
        /// </summary>
        public UdpProtocol(int port, UdpPacket onReceive_server, IPType listenTo) : base(port)
        {
            serverSideReceive = onReceive_server;
            clientSideReceive = null;
            isServerClient = true;

            try
            {
                client = new UdpClient(listenTo switch
                {
                    IPType.ExclusiveIPv4 => AddressFamily.InterNetwork,
                    _ => AddressFamily.InterNetworkV6
                });
                Connected = true;

                // Set the UDP client to reuse the address and port (optional)
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                //  Set IPType
                switch (listenTo)
                {
                    case IPType.ExclusiveIPv4:
                        client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                        break;

                    case IPType.ExclusiveIPv6:
                        client.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                        break;

                    default:
                        client.Client.DualMode = true;
                        client.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                        break;
                }

                // Listens to udp signals
                Listen();
            }
            catch (Exception ex)
            {
                Close();

                Debug.LogError($"Was not able to establish a udp connection: {ex.Message}. Closing server.");
                return;
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
        public void Send<T>(ref short key, T value, IPEndPoint destination)
        {
            ValidateConnection();

            if (destination != null)
            {
                byte[] bytes = PackageS2C(ref key, ref value);
                client.Send(bytes, bytes.Length, destination);
            }
        }

        /// <summary> 
        /// Sends data through connection (Client Side) 
        /// </summary>
        public void Send<T>(ref short key, T value, ushort safetyId)
        {
            ValidateConnection();

            byte[] bytes = PackageC2S(ref key, ref value, ref safetyId);
            client.Send(bytes, bytes.Length);
        }
        #endregion

        #region Receive Data    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Receive packets as long as client is connected 
        /// </summary>
        private async void Listen()
        {
            while (Connected && Cancel.IsCancellationRequested == false)
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
            UdpReceiveResult result = await client.ReceiveAsync(Cancel);
            byte[] buffer = result.Buffer;

            // Invoke callback
            if (isServerClient)
            {
                // Decode bytes
                if (UnpackC2S(buffer, out var package, out var safetyId))
                    serverSideReceive?.Invoke(ref package, result.RemoteEndPoint, safetyId);
            }

            else
            {
                // Decode bytes
                if (UnpackS2C(buffer, out var package))
                    clientSideReceive?.Invoke(ref package);
            }
        }
        #endregion

        #region End Connection  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Closes local network elements 
        /// </summary>
        public override void Close()
        {
            client?.Close();
            client = null;

            base.Close();
        }
        #endregion

        #region Packaging
        /// <summary>
        /// Packets from server, sent to client
        /// </summary>
        public static byte[] PackageS2C<T>(ref short key, ref T value)
        {
            // Convert to bytes
            var bytes = value == null ? Array.Empty<byte>() : value.Buffer();

            // Establish streams
            using MemoryStream strm = new();
            using BinaryWriter wrtr = new(strm);

            // Write key
            wrtr.Write(key);

            // Write custom values
            wrtr.Write(bytes);

            // Close streams
            strm.Close();
            wrtr.Close();

            return strm.ToArray();
        }

        /// <summary>
        /// Packets from client, sent to server
        /// </summary>
        public static byte[] PackageC2S<T>(ref short key, ref T value, ref ushort udpSafety)
        {
            // Convert to bytes
            var bytes = value == null ? Array.Empty<byte>() : value.Buffer();

            // Establish streams
            using MemoryStream strm = new();
            using BinaryWriter wrtr = new(strm);

            // Write constant data
            wrtr.Write(udpSafety);
            wrtr.Write(key);

            // Write custom values
            wrtr.Write(bytes);

            // Close streams
            strm.Close();
            wrtr.Close();

            return strm.ToArray();
        }

        /// <summary>
        /// Packets from client, received by server
        /// </summary>
        public static bool UnpackC2S(byte[] buffer, out NetworkPackage package, out ushort udpSafety)
        {
            // Buffer could not be read
            if (buffer == null || buffer.Length < 2)
            {
                udpSafety = default;
                package = default;
                return false;
            }

            // Establish streams
            using MemoryStream strm = new(buffer);
            using BinaryReader rdr = new(strm);

            // Udp safety
            udpSafety = rdr.ReadUInt16();

            // Read key and contents
            package = new(rdr.ReadInt16(), rdr.ReadBytes(buffer.Length - 2), Method.Udp);

            // Close streams
            strm.Close();
            rdr.Close();

            return true;
        }

        /// <summary>
        /// Packets from server, received by client
        /// </summary>
        public static bool UnpackS2C(byte[] buffer, out NetworkPackage package)
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
            package = new(rdr.ReadInt16(), rdr.ReadBytes(buffer.Length - 2), Method.Udp);

            // Close streams
            strm.Close();
            rdr.Close();

            return true;
        }
        #endregion
    }
}