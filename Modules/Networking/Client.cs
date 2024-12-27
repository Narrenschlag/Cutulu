namespace Cutulu.Networking
{
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Threading;
    using System.Net;
    using System.IO;
    using System;

    using Cutulu;

    /// <summary>
    /// Client class for sending and receiving packages from and to servers.
    /// </summary>
    public partial class Client
    {
        public bool Connected { get; private set; } = false;

        public TcpClient TcpClient { get; private set; }
        public UdpClient UdpClient { get; private set; }
        private IPEndPoint UdpEndpoint { get; set; }

        public string Host { get; private set; }
        public int TcpPort { get; private set; }
        public int UdpPort { get; private set; }

        public event Action<short, byte[]> ReceivedTcp, ReceivedUdp, ReceivedAny;
        public event Action ConnectionEstablished, ConnectionClosed;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken CancellationToken;
        public long UID { get; private set; }

        public Client() { }

        public Client(string host, int tcpPort, int udpPort)
        {
            _ = Connect(host, tcpPort, udpPort);
        }

        public virtual async Task Connect(string host, int tcpPort, int udpPort)
        {
            // Stop currently running host
            await Disconnect(1);

            if (IPAddress.TryParse(host, out var address) == false)
                return;

            // Assign values
            TcpPort = tcpPort;
            UdpPort = udpPort;
            Host = host;

            // Establish cancellation token
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = cancellationTokenSource.Token;

            // Setup tcp client
            TcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            TcpClient.Client.DualMode = true;

            // Connect to host
            await TcpClient.ConnectAsync(host, tcpPort, CancellationToken);

            // Connection failed
            if (TcpClient.Connected == false)
            {
                Debug.LogError($"Couldn't connect host.");
                await Disconnect(2);
                return;
            }

            // Setup udp client
            if (udpPort >= 0)
            {
                UdpClient = new(AddressFamily.InterNetworkV6);
                UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                UdpClient.Client.DualMode = true;

                // Assign endpoint
                UdpEndpoint = new(address, udpPort);

                // Connect with the host
                UdpClient.Connect(host, UdpPort);

                // Wait for udp packages
                AcceptUdp();

                // Check for local udp endpoint and tcp network stream
                if (UdpClient.Client.LocalEndPoint is IPEndPoint endpoint && TcpClient.GetStream() is NetworkStream stream)
                {
                    // Assign port
                    var port = endpoint.Port;

                    // Write setup data into stream
                    await stream.WriteAsync(new[] { (byte)ConnectionTypeEnum.Connect });
                    await stream.WriteAsync(BitConverter.GetBytes(port));

                    // Send setup data to host
                    await stream.FlushAsync();

                    // Wait for response
                    var buffer = new byte[8];
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                    UID = BitConverter.ToInt32(buffer);

                    // Accept tcp from here on as we have to wait for the UID first.
                    AcceptTcp();

                    //Debug.Log($"received-uid: {UID}({bytesRead}/{buffer.Length} bytes)");
                    Debug.Log($"Client connected. [UID={UID} udp={port} tcp={((IPEndPoint)TcpClient.Client.LocalEndPoint).Port}]");

                    // Finish
                    Connected = true;
                    ConnectionEstablished?.Invoke();
                }

                // Local udp endpoint and/or network stream were not found or incorrectly setup
                else
                {
                    Debug.LogError($"Client udp port cannot be read.");
                    await Disconnect(4);
                    return;
                }
            }
        }

        public virtual async Task Disconnect(int exitCode = 0)
        {
            cancellationTokenSource?.Cancel();
            Connected = false;

            TcpClient?.Close();
            UdpClient?.Close();

            await Task.Delay(1);

            if (exitCode != 1)
            {
                Debug.Log($"Disconnected from host [exitCode:{exitCode}]");

                ConnectionClosed?.Invoke();
            }
        }

        #region Udp

        public virtual async Task WriteUdpAsync(short key, byte[] buffer)
        {
            if (Connected == false || UdpClient == null || UdpEndpoint == null) return;

            using var memory = new MemoryStream();

            await memory.WriteAsync(BitConverter.GetBytes(key), CancellationToken);
            await memory.WriteAsync(buffer, CancellationToken);

            await UdpClient.SendAsync(memory.ToArray(), CancellationToken);
        }

        public virtual void WriteUdp(short key, byte[] buffer)
        {
            if (Connected == false || UdpClient == null || UdpEndpoint == null) return;

            using var memory = new MemoryStream();

            memory.Write(BitConverter.GetBytes(key));
            memory.Write(buffer);

            UdpClient.Send(memory.ToArray());
        }

        private async void AcceptUdp()
        {
            while (TcpClient.Connected && UdpClient != null)
            {
                if (UdpClient.Available > 0)
                {
                    try
                    {
                        var udpReceiveResult = await UdpClient.ReceiveAsync(CancellationToken);

                        if (udpReceiveResult.Buffer.Length >= 2)
                        {
                            using var stream = new MemoryStream(udpReceiveResult.Buffer);
                            using var reader = new BinaryReader(stream);

                            ReceiveUdp(reader.ReadInt16(), reader.ReadBytes((int)(stream.Length - stream.Position)));
                        }
                    }

                    catch (Exception ex)
                    {
                        Debug.LogError($"CLIENT_UDP_ERROR({ex.GetType().Name}, {ex.Message})\n{ex.StackTrace}");
                        continue;
                    }
                }

                else
                    await Task.Delay(10);
            }
        }

        private void ReceiveUdp(short key, byte[] buffer)
        {
            switch (key)
            {
                default:
                    ReceivedUdp?.Invoke(key, buffer);
                    ReceivedAny?.Invoke(key, buffer);
                    break;
            }
        }

        #endregion

        #region Tcp

        public virtual async Task WriteTcpAsync(short key, byte[] buffer)
        {
            if (Connected == false || TcpClient.GetStream() is not NetworkStream stream) return;

            using var memory = new MemoryStream();
            await memory.WriteAsync(BitConverter.GetBytes(buffer.Length + 2));
            await memory.WriteAsync(BitConverter.GetBytes(key));
            await memory.WriteAsync(buffer);

            await stream.WriteAsync(memory.ToArray());
            await stream.FlushAsync();
        }

        public virtual void WriteTcp(short key, byte[] buffer)
        {
            if (Connected == false || TcpClient.GetStream() is not NetworkStream stream) return;

            stream.Write(BitConverter.GetBytes(buffer.Length + 2));
            stream.Write(BitConverter.GetBytes(key));
            stream.Write(buffer);

            stream.Flush();
        }

        private async void AcceptTcp()
        {
            if (TcpClient.GetStream() is not NetworkStream stream) return;

            while (TcpClient.Connected)
            {
                try
                {
                    var buffer = new byte[4];

                    if (await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken) < 2) throw new IOException("CLIENT_LOST_CONNECTION_TO_HOST");

                    var length = BitConverter.ToInt32(buffer);
                    if (length < 2) continue;

                    buffer = new byte[2];
                    await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken);
                    var key = BitConverter.ToInt16(buffer);

                    buffer = new byte[length -= 2];
                    await stream.ReadAsync(buffer.AsMemory(0, length), CancellationToken);

                    ReceiveTcp(key, buffer);
                }

                catch (Exception ex)
                {
                    switch (ex)
                    {
                        case IOException _ex when _ex.Message == "CLIENT_LOST_CONNECTION_TO_HOST":
                            Debug.LogR($"[color=red]{ex.Message}");
                            break;

                        case OperationCanceledException:
                            break;

                        default:
                            Debug.LogError($"CLIENT_TCP_ERROR({ex.GetType().Name}, {ex.Message})\n{ex.StackTrace}");
                            break;
                    }

                    break;
                }
            }

            await Disconnect(3);
        }

        private void ReceiveTcp(short key, byte[] buffer)
        {
            switch (key)
            {
                default:
                    ReceivedTcp?.Invoke(key, buffer);
                    ReceivedAny?.Invoke(key, buffer);
                    break;
            }
        }

        #endregion
    }
}