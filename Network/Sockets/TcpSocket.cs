namespace Cutulu.Network.Sockets
{
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Threading;
    using System.IO;
    using System;

    using Core;

    public partial class TcpSocket
    {
        public readonly TcpClient Client;

        public bool IsConnected => Client.Connected;
        public Socket Socket => Client.Client;

        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        public string Address { get; private set; }
        public int Port { get; private set; }

        private bool Receiving { get; set; }

        public Action<TcpSocket> Connected, Disconnected;

        /// <summary>
        /// Constructs simple tcp client capable of IPv4 and IPv6.
        /// </summary>
        public TcpSocket()
        {
            Client = new TcpClient(AddressFamily.InterNetworkV6);

            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Socket.DualMode = true;
        }

        /// <summary>
        /// Constructs simple tcp client capable of IPv4 and IPv6 using existing socket.
        /// </summary>
        public TcpSocket(TcpClient client)
        {
            Client = client;
        }

        /// <summary>
        /// Connect to host async.
        /// </summary>
        public virtual async Task<bool> Connect(string address, int port, int timeout = 5000, bool enableMultiThreading = false)
        {
            Disconnect(1);

            _ = Client.ConnectAsync(address, port, Token = (TokenSource = new()).Token);

            while (timeout-- > 0 && IsConnected == false)
            {
                await Task.Delay(1);
            }

            if (IsConnected == false)
            {
                Disconnect(2);
                return false;
            }

            lock (this)
            {
                Receiving = false;
                Address = address;
                Port = port;

                Connected?.Invoke(this);

                return true;
            }
        }

        /// <summary>
        /// Connect to host async.
        /// Tries again for given amount of runs, adding timeoutStep to the timeout each time.
        /// </summary>
        public virtual async Task<bool> Connect(string address, int port, int timeoutStep, int timeoutRuns, bool enableMultiThreading = false)
        {
            for (int i = 0; i < timeoutRuns && IsConnected == false; i++)
            {
                await Connect(address, port, timeoutStep * (i + 1), enableMultiThreading);
            }

            return IsConnected;
        }

        /// <summary>
        /// Disconnects from host and terminates all running processes.
        /// </summary>
        public virtual void Disconnect(byte exitCode = 0)
        {
            TokenSource?.Cancel();

            Token = CancellationToken.None;
            TokenSource = null;

            if (IsConnected)
            {
                Socket.Disconnect(true);

                Disconnected?.Invoke(this);
            }
        }

        /// <summary>
        /// Disposes this client. Disconnects from host and terminates all processes.
        /// Careful: This may render this client unusable. If you just want to disconnect, use Disconnect(). 
        /// </summary>
        public virtual void Close()
        {
            Disconnect(3);

            Client?.Close();
        }

        /// <summary>
        /// Sends data to host.
        /// </summary>
        public virtual async Task<bool> SendAsync(params byte[][] buffers)
        {
            if (IsConnected == false || buffers.IsEmpty() || Client.GetStream() is not NetworkStream stream) return false;

            var token = Token;

            for (int i = 0; i < buffers.Length && token.IsCancellationRequested == false; i++)
            {
                if (buffers[i].NotEmpty())
                    await stream.WriteAsync(buffers[i], token);
            }

            if (token.IsCancellationRequested == false)
                await stream.FlushAsync(token);

            return token.IsCancellationRequested == false;
        }

        /// <summary>
        /// Sends data to host async.
        /// </summary>
        public virtual bool Send(params byte[][] buffers)
        {
            if (IsConnected == false || buffers.IsEmpty() || Client.GetStream() is not NetworkStream stream) return false;

            for (int i = 0; i < buffers.Length; i++)
            {
                if (buffers[i].NotEmpty())
                    stream.Write(buffers[i]);
            }

            stream.Flush();

            return true;
        }

        /// <summary>
        /// Receives data, writes bytes to memory and polls.
        /// </summary>
        public virtual async Task<(bool Success, byte[] Buffer)> Receive(int length)
        {
            if (IsConnected && Receiving == false && Client.GetStream() is NetworkStream stream)
            {
                Receiving = true;

                var buffer = new byte[length];

                try
                {
                    Debug.Log($"Waiting for {length} bytes...");

                    if (await stream.ReadAsync(buffer, Token) < length)
                        throw new IOException("CLIENT_LOST_CONNECTION_TO_HOST");

                    Debug.LogR($"[color=green]Received {length} bytes.");

                    Receiving = false;
                    return (true, buffer);
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

                    Disconnect(255);
                }
            }

            return (false, Array.Empty<byte>());
        }
    }
}