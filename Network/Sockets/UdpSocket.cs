namespace Cutulu.Network.Sockets
{
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Threading;
    using System.Net;
    using System.IO;
    using System;

    using Core;

    public partial class UdpSocket
    {
        public IPEndPoint Endpoint { get; private set; }
        public UdpClient Client { get; private set; }

        public bool IsConnected => Socket != null; //&& Socket.Connected;
        public Socket Socket => Client?.Client;

        public IPEndPoint GetLocalEndpoint() => Socket.LocalEndPoint as IPEndPoint;
        public IPEndPoint GetRemoteEndpoint() => Socket.RemoteEndPoint as IPEndPoint;

        public string Address { get; private set; }
        public int Port { get; private set; }

        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        private bool Receiving { get; set; }
        private UdpHost Host { get; set; }

        public Action<UdpSocket> Connected, Disconnected;

        public UdpSocket() { }

        /// <summary>
        /// Constructs simple tcp client capable of IPv4 and IPv6 using existing socket.
        /// </summary>
        public UdpSocket(UdpHost host)
        {
            Host = host;
        }

        #region Callable Functions

        /// <summary>
        /// Starts udp client for host purpose.
        /// </summary>
        public virtual void Start(int port)
        {
            Disconnect(1);

            if (Client != null) return;

            Client = new(AddressFamily.InterNetworkV6);

            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Socket.DualMode = true;

            Socket.Bind(new IPEndPoint(IPAddress.IPv6Any, Port = port));
        }

        /// <summary>
        /// Connects to host.
        /// </summary>
        public virtual async Task Connect(string address, int port)
        {
            Disconnect(1);

            // Wait until disconnected
            while (IsConnected) await Task.Delay(1);

            if (IPAddress.TryParse(Address = address, out var ipAddress) == false)
                return;

            Token = (TokenSource = new()).Token;

            if (Client == null)
            {
                Client = new(AddressFamily.InterNetworkV6);

                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Socket.DualMode = true;
            }

            // Assign endpoint
            Endpoint = new(ipAddress, Port = port);
            Receiving = false;

            // Connect with the host
            Client.Connect(address, port);
        }

        /// <summary>
        /// Disconnects from host and terminates all running processes.
        /// </summary>
        public virtual void Disconnect(int exitCode = 0)
        {
            TokenSource?.Cancel();

            Token = CancellationToken.None;
            TokenSource = null;
            Receiving = false;

            if (IsConnected)
            {
                // Dispose client
                Client.Close();
                Client = null;

                Disconnected?.Invoke(this);
            }
        }

        /// <summary>
        /// Disposes this client. Disconnects from host and terminates all processes.
        /// </summary>
        public virtual void Close()
        {
            Disconnect(3);
        }

        /// <summary>
        /// Sends data to host.
        /// </summary>
        public virtual void Send(params byte[][] buffers)
        {
            if (IsConnected == false || Endpoint == null || buffers.IsEmpty()) return;

            using var memory = new MemoryStream();

            for (int i = 0; i < buffers.Length; i++)
            {
                if (buffers[i].NotEmpty())
                    memory.Write(buffers[i]);
            }

            Client.Send(memory.ToArray());
        }

        /// <summary>
        /// Sends data to host.
        /// </summary>
        public virtual void Send(IPEndPoint[] endpoints, params byte[][] buffers)
        {
            if (IsConnected == false || endpoints.IsEmpty() || buffers.IsEmpty()) return;

            using var memory = new MemoryStream();
            var token = Token;

            for (int i = 0; i < buffers.Length; i++)
            {
                if (buffers[i].NotEmpty())
                    memory.Write(buffers[i]);
            }

            for (int i = 0; i < endpoints.Length; i++)
            {
                Client.Send(memory.ToArray(), endpoints[i]);
            }
        }

        /// <summary>
        /// Sends data to host async.
        /// </summary>
        public virtual async Task<bool> SendAsync(params byte[][] buffers)
        {
            if (IsConnected == false || Endpoint == null || buffers.IsEmpty()) return false;

            using var memory = new MemoryStream();
            var token = Token;

            for (int i = 0; i < buffers.Length && token.IsCancellationRequested == false; i++)
            {
                if (buffers[i].NotEmpty())
                    await memory.WriteAsync(buffers[i], token);
            }

            if (token.IsCancellationRequested == false)
                await Client.SendAsync(memory.ToArray(), token);

            return token.IsCancellationRequested == false;
        }

        /// <summary>
        /// Sends data to host async.
        /// </summary>
        public virtual async Task<bool> SendAsync(IPEndPoint[] endpoints, params byte[][] buffers)
        {
            if (IsConnected == false || endpoints.IsEmpty() || buffers.IsEmpty()) return false;

            using var memory = new MemoryStream();
            var token = Token;

            for (int i = 0; i < buffers.Length && token.IsCancellationRequested == false; i++)
            {
                if (buffers[i].NotEmpty())
                    await memory.WriteAsync(buffers[i], token);
            }

            for (int i = 0; i < endpoints.Length && token.IsCancellationRequested == false; i++)
            {
                await Client.SendAsync(memory.ToArray(), endpoints[i], token);
            }

            return token.IsCancellationRequested == false;
        }

        /// <summary>
        /// Receive udp package if available.
        /// </summary>
        public async Task<(bool Success, byte[] Buffer, IPEndPoint RemoteEndPoint)> Receive()
        {
            var token = Token;

            if (IsConnected && Receiving == false)
            {
                Receiving = true;

                try
                {
                    var udpReceiveResult = await Client.ReceiveAsync(token);

                    if (token.IsCancellationRequested == false && udpReceiveResult.Buffer.Length > 1)
                    {
                        Receiving = false;
                        return (true, udpReceiveResult.Buffer, udpReceiveResult.RemoteEndPoint);
                    }
                }

                catch (Exception ex)
                {
                    var prefix = Host != null ? "HOST" : "CLIENT";

                    switch (ex)
                    {
                        case IOException _ex when _ex.Message.StartsWith("LOST_CONNECTION"):
                            Debug.LogR($"[color=red]{prefix}_UDP_{ex.Message}");
                            break;

                        case OperationCanceledException:
                            break;

                        default:
                            if (ex.StackTrace.Contains("CancellationToken")) break;

                            Debug.LogError($"{prefix}_UDP_ERROR({ex.GetType().Name}, {ex.Message})\n{ex.StackTrace}");
                            break;
                    }
                }

                Receiving = false;
            }

            return (false, Array.Empty<byte>(), default);
        }

        #endregion
    }
}