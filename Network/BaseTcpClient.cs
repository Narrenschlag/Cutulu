namespace Cutulu.Network
{
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Threading;
    using Cutulu.Core;
    using System;
    using System.IO;

    public partial class BaseTcpClient
    {
        public const int BufferSize = 2048; // Read incomming data in 2kb chunks

        public readonly TcpClient Client;

        public bool IsConnected => Client.Connected;
        public Socket Socket => Client.Client;

        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        public string Address { get; private set; }
        public int Port { get; private set; }

        private bool EnableMultiThreading { get; set; }
        private Thread Thread { get; set; }

        public Action<BaseTcpClient, byte[], int> ReceivedBuffer;
        public Action<BaseTcpClient> Connected, Disconnected;

        /// <summary>
        /// Constructs simple tcp client capable of IPv4 and IPv6.
        /// </summary>
        public BaseTcpClient()
        {
            Client = new TcpClient(AddressFamily.InterNetworkV6);

            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Socket.DualMode = true;
        }

        /// <summary>
        /// Constructs simple tcp client capable of IPv4 and IPv6 using existing socket.
        /// </summary>
        public BaseTcpClient(TcpClient client)
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

            Address = address;
            Port = port;

            if (EnableMultiThreading = enableMultiThreading)
                Thread = new(new ThreadStart(Process));
            else Process();

            Connected?.Invoke(this);

            return true;
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
            Thread?.Interrupt();
            Thread?.Join();

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
        /// Receives data, writes bytes to memory, polls .
        /// </summary>
        protected virtual async void Process()
        {
            byte[] buffer = null;
            var token = Token;
            var length = 0;

            try
            {
                // TODO: Doesn't work yet.
                if (Client.GetStream() is NetworkStream stream && stream.DataAvailable)
                    (buffer, length) = await HandleTraffic(stream);

                else await Task.Delay(1);
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

            if (EnableMultiThreading == false && token.IsCancellationRequested) return;

            if (length > 0 && buffer != null)
            {
                lock (this)
                {
                    ReceivedBuffer?.Invoke(this, buffer, length);
                }
            }

            Process();
        }

        protected virtual async Task<(byte[] Buffer, int Length)> HandleTraffic(NetworkStream stream)
        {
            Debug.LogError($"Data available!");
            var buffer = new byte[BufferSize];

            return (buffer, await stream.ReadAsync(buffer, Token));
        }
    }
}