namespace Cutulu.Core.Networking
{
    using System.Net.Sockets;
    using System.Net;

    using System.Threading.Tasks;
    using System.Threading;

    using System;
    using Cutulu.Core;

    /// <summary>
    /// Ping class for sending and receiving pings to servers.
    /// </summary>
    public partial class Ping
    {
        public TcpClient TcpClient { get; private set; }

        public string Host { get; private set; }
        public int TcpPort { get; private set; }

        public event Action<byte[]> PingAwnsered;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken CancellationToken;

        public Ping() { }

        public Ping(string host, int tcpPort)
        {
            _ = ConnectAsync(host, tcpPort);
        }

        public virtual async Task ConnectAsync(string host, int tcpPort)
        {
            // Stop currently running host
            Disconnect(1);

            if (IPAddress.TryParse(host, out var address) == false)
                return;

            // Assign values
            TcpPort = tcpPort;
            Host = host;

            // Establish cancellation token
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = cancellationTokenSource.Token;

            //Setup tcp client
            TcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            TcpClient.Client.DualMode = true;

            // Connect tcp client
            await TcpClient.ConnectAsync(host, tcpPort, CancellationToken);

            // Catch error
            if (TcpClient.Connected == false)
            {
                Debug.LogError($"Couldn't connect host.");
                Disconnect(2);
                return;
            }

            // Get NetworkStream
            else if (TcpClient.GetStream() is NetworkStream stream)
            {
                // Send ping type
                await stream.WriteAsync(new[] { (byte)ConnectionTypeEnum.Ping });
                await stream.FlushAsync();

                // Receive length of buffer
                var buffer = new byte[2];
                await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken);

                // Receive buffer with ping data
                buffer = new byte[BitConverter.ToUInt16(buffer)];
                await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken);

                // Invoke ping awnsered event
                PingAwnsered?.Invoke(buffer);
            }

            // Catch error
            else
            {
                Debug.LogError($"Couldn't read network stream.");
                Disconnect(4);
                return;
            }
        }

        public virtual void Disconnect(int exitCode = 0)
        {
            cancellationTokenSource?.Cancel();

            TcpClient?.Close();

            if (exitCode != 1)
            {
                Debug.Log($"Disconnected from host [exitCode:{exitCode}]");
            }
        }
    }
}