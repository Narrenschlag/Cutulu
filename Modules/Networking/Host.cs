namespace Cutulu.Core.Networking
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Threading;
    using System.Net;
    using System.IO;
    using System;

    using Cutulu.Core;

    public partial class Host
    {
        public readonly Dictionary<IPEndPoint, Connection> UdpEndpoints = new();
        public readonly Dictionary<long, Connection> Connections = new();
        public bool Running { get; private set; } = false;
        public bool Ready { get; private set; } = false;

        public TcpListener TcpListener { get; private set; }
        public UdpClient UdpHost { get; private set; }

        public int TcpPort { get; private set; }
        public int UdpPort { get; private set; }

        public byte[] PingBuffer { get; set; }

        public Action<Connection, short, byte[]> ReceivedTcp, ReceivedUdp, ReceivedAny;
        public Action<Connection> ConnectionEstablished, ConnectionClosed;
        public Action HostStarted, HostStopped;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken CancellationToken;
        private long lastUID;

        public Host() { }

        public virtual async Task Start(int tcpPort, int udpPort)
        {
            // Stop currently running host
            await Stop(1);

            // Assign values
            TcpPort = tcpPort;
            UdpPort = udpPort;
            Running = true;
            Ready = false;

            // Establish cancellation token
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = cancellationTokenSource.Token;

            Debug.Log($"Starting host...");

            #region Udp

            // Establish udp listener
            UdpHost = new UdpClient(AddressFamily.InterNetworkV6);
            UdpHost.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            UdpHost.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            UdpHost.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, udpPort));

            Debug.Log($"[udp running on {udpPort}]");

            AcceptUdp();

            #endregion

            #region Tcp

            // Establish tcp listener
            TcpListener = new TcpListener(IPAddress.IPv6Any, tcpPort);
            TcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            TcpListener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            TcpListener.Start();

            Debug.Log($"[tcp running on {tcpPort}]");

            WaitForTcp();

            #endregion

            // Finish
            Ready = true;
            HostStarted?.Invoke();
        }

        public virtual async Task Stop(int exitCode = 0)
        {
            if (Running == false) return;

            var wasReady = Ready;
            Running = false;
            Ready = false;
            lastUID = 0;

            foreach (var connection in Connections.Values)
            {
                connection?.Close();
            }

            UdpEndpoints.Clear();
            Connections.Clear();

            cancellationTokenSource.Cancel();
            TcpListener.Stop();
            UdpHost.Close();

            await Task.Delay(1);

            if (exitCode != 1)
            {
                Debug.Log($"Host closed with exitCode={exitCode}: {(wasReady ? $"Cancelled host setup" : $"Stopped host")}");

                HostStopped?.Invoke();
            }
        }

        #region Udp

        private async void AcceptUdp()
        {
            while (Running && UdpHost != null)
            {
                if (UdpHost.Available > 0)
                {
                    try
                    {
                        var udpReceiveResult = await UdpHost.ReceiveAsync(CancellationToken);

                        if (UdpEndpoints.NotEmpty() && udpReceiveResult.Buffer.Length >= 2)
                        {
                            if (UdpEndpoints.TryGetValue(udpReceiveResult.RemoteEndPoint, out var connection))
                            {
                                using var stream = new MemoryStream(udpReceiveResult.Buffer);
                                using var reader = new BinaryReader(stream);

                                connection.ReceiveUdp(reader.ReadInt16(), reader.ReadBytes((int)(stream.Length - stream.Position)));
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        Debug.LogError($"HOST_UDP_ERROR({ex.GetType().Name}, {ex.Message})\n{ex.StackTrace}");
                        continue;
                    }
                }

                else
                {
                    await Task.Delay(10);
                }
            }
        }

        #endregion

        #region Tcp

        private async void WaitForTcp()
        {
            while (Running)
            {
                var client = await TcpListener.AcceptTcpClientAsync(CancellationToken);
                AcceptTcp(client);
            }
        }

        private async void AcceptTcp(TcpClient client)
        {
            if (client == null) return;

            if (Running)
            {
                var stream = client.GetStream();

                // Receive reason for connection
                var buffer = new byte[1];
                await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken);

                switch ((ConnectionTypeEnum)buffer[0])
                {
                    // Return data if existing
                    case ConnectionTypeEnum.Ping:
                        await stream.WriteAsync(BitConverter.GetBytes(PingBuffer.Size()));
                        await stream.WriteAsync(PingBuffer ?? Array.Empty<byte>(), CancellationToken);
                        await stream.FlushAsync(CancellationToken);
                        client?.Close();
                        return;

                    // Continue with setting up the connection
                    case ConnectionTypeEnum.Connect:
                        break;

                    // Close unauthorized client
                    // Stop the process
                    default:
                        client?.Close();
                        return;
                }

                // Receive udp port
                buffer = new byte[4];
                await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken);

                var port = BitConverter.ToInt32(buffer);
                var connection = default(Connection);

                if (client.Client.RemoteEndPoint is IPEndPoint endpoint)
                {
                    var udpEndpoint = new IPEndPoint(endpoint.Address, port);
                    var uid = lastUID++;

                    // Awnser with UID
                    await stream.WriteAsync(BitConverter.GetBytes(uid), CancellationToken);
                    await stream.FlushAsync(CancellationToken);

                    UdpEndpoints[udpEndpoint] = connection = new Connection(ref client, ref udpEndpoint, uid, this, CancellationToken);
                    ConnectionEstablished?.Invoke(connection);
                }

                // Two step receiving
                if (connection != null)
                {
                    while (Running && stream != null && client.Connected)
                    {
                        try
                        {
                            buffer = new byte[4];
                            if (await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), connection.CancellationToken) < 2) throw new IOException("HOST_LOST_CONNECTION_TO_CLIENT");

                            var length = BitConverter.ToInt32(buffer);
                            if (length < 2) continue;

                            buffer = new byte[2];
                            await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), connection.CancellationToken);
                            var key = BitConverter.ToInt16(buffer);

                            buffer = new byte[length -= 2];
                            await stream.ReadAsync(buffer.AsMemory(0, length), connection.CancellationToken);

                            connection.ReceiveTcp(key, buffer);
                        }

                        catch (Exception ex)
                        {
                            switch (ex)
                            {
                                case IOException _ex when _ex.Message == "HOST_LOST_CONNECTION_TO_CLIENT":
                                    Debug.LogR($"[color=red]{ex.Message}");
                                    break;

                                case OperationCanceledException:
                                    break;

                                default:
                                    Debug.LogError($"HOST_TCP_ERROR({ex.GetType().Name}, {ex.Message})\n{ex.StackTrace}");
                                    break;
                            }

                            break;
                        }
                    }
                }

                // Close connection
                connection?.Close();
            }

            // Close client
            client?.Close();
        }

        #endregion
    }
}