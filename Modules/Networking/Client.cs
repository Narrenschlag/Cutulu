namespace Cutulu.Networking
{
    using System.Net.Sockets;
    using System.Net;

    using System.Threading.Tasks;
    using System.Threading;

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
            _ = ConnectAsync(host, tcpPort, udpPort);
        }

        public virtual async Task ConnectAsync(string host, int tcpPort, int udpPort)
        {
            // Stop currently running host
            Disconnect(1);

            if (IPAddress.TryParse(host, out var address) == false)
                return;

            // Assign values
            TcpPort = tcpPort;
            UdpPort = udpPort;
            Host = host;

            // Establish cancellation token
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = cancellationTokenSource.Token;

            #region Tcp (0/1)

            TcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            TcpClient.Client.DualMode = true;

            await TcpClient.ConnectAsync(host, tcpPort, CancellationToken);

            if (TcpClient.Connected == false)
            {
                Debug.LogError($"Couldn't connect host.");
                Disconnect(2);
                return;
            }

            #endregion

            #region Udp

            UdpClient = new(AddressFamily.InterNetworkV6);
            UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            UdpClient.Client.DualMode = true;

            UdpEndpoint = new(address, udpPort);

            // Connect with the host
            UdpClient.Connect(host, UdpPort);

            udp();
            async void udp()
            {
                while (TcpClient.Connected && UdpClient != null)
                {
                    if (UdpClient.Available > 0)
                    {
                        try
                        {
                            AcceptUdp(await UdpClient.ReceiveAsync(CancellationToken));
                        }

                        catch (Exception ex)
                        {
                            Debug.LogError($"client> UDP receive error: {ex.Message}\n{ex.StackTrace}");
                            continue;
                        }
                    }

                    else
                        await Task.Delay(10);
                }
            }

            #endregion

            #region Setup + Tcp(1/1)

            if (UdpClient.Client.LocalEndPoint is IPEndPoint endpoint && TcpClient.GetStream() is NetworkStream stream)
            {
                var port = endpoint.Port;

                await stream.WriteAsync(new[] { (byte)ConnectionTypeEnum.Connect });
                await stream.WriteAsync(BitConverter.GetBytes(port));
                await stream.FlushAsync();

                var buffer = new byte[8];
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                UID = BitConverter.ToInt32(buffer);

                // Accept tcp from here on as we have to wait for the UID first.
                AcceptTcp();

                Debug.Log($"received-uid: {UID}({bytesRead}/{buffer.Length} bytes)");

                Debug.Log($"Client setup complete. [UID={UID} udp={port} tcp={((IPEndPoint)TcpClient.Client.LocalEndPoint).Port}]");
            }

            else
            {
                Debug.LogError($"Client udp port cannot be read.");
                Disconnect(4);
                return;
            }

            #endregion

            Connected = true;
            ConnectionEstablished?.Invoke();
        }

        protected virtual async void AcceptTcp()
        {
            if (TcpClient.GetStream() is not NetworkStream stream) return;

            while (TcpClient.Connected)
            {
                try
                {
                    var buffer = new byte[4];

                    if (await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken) < 2) throw new IOException("connection corrupted");

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
                    Debug.LogError($"client> TCP receive error: {ex.Message}\n{ex.StackTrace}");
                    break;
                }
            }

            Disconnect(3);
        }

        protected virtual void AcceptUdp(UdpReceiveResult udpReceiveResult)
        {
            if (udpReceiveResult.Buffer.Length < 2) return;

            using var stream = new MemoryStream(udpReceiveResult.Buffer);
            using var reader = new BinaryReader(stream);

            ReceiveUdp(reader.ReadInt16(), reader.ReadBytes((int)(stream.Length - stream.Position)));
        }

        public virtual void Disconnect(int exitCode = 0)
        {
            cancellationTokenSource?.Cancel();
            Connected = false;

            TcpClient?.Close();
            UdpClient?.Close();

            if (exitCode != 1)
            {
                Debug.Log($"Disconnected from host [exitCode:{exitCode}]");

                ConnectionClosed?.Invoke();
            }
        }

        #region Udp
        public virtual void ReceiveUdp(short key, byte[] buffer)
        {
            switch (key)
            {
                default:
                    ReceivedUdp?.Invoke(key, buffer);
                    ReceivedAny?.Invoke(key, buffer);
                    break;
            }
        }

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
        #endregion

        #region Tcp
        public virtual void ReceiveTcp(short key, byte[] buffer)
        {
            switch (key)
            {
                default:
                    ReceivedTcp?.Invoke(key, buffer);
                    ReceivedAny?.Invoke(key, buffer);
                    break;
            }
        }

        public virtual async Task WriteTcpAsync(short key, byte[] buffer)
        {
            if (Connected == false || TcpClient.GetStream() is not NetworkStream stream) return;

            using var memory = new MemoryStream();
            await memory.WriteAsync(BitConverter.GetBytes((buffer.Length + 2)));
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
        #endregion
    }
}