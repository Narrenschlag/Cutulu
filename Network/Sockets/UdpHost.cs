namespace Cutulu.Network.Sockets
{
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Threading;
    using System.Net;
    using System;

    public partial class UdpHost
    {
        public UdpSocket Listener { get; private set; }
        public int Port { get; private set; }

        public bool IsListening => Socket != null;
        public Socket Socket => Listener?.Socket;

        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        public bool UseRouterPortForwarding { get; set; }
        private RouterPortForwarding RouterPortForwarder { get; set; }

        public Action<IPEndPoint, byte[]> Received;
        public Action<UdpHost> Started, Stopped;

        /// <summary>
        /// Constructs simple udp listener capable of IPv4 and IPv6.
        /// </summary>
        public UdpHost() { }

        #region Callable Functions

        /// <summary>
        /// Starts udp listener.
        /// </summary>
        public virtual void Start(int port)
        {
            // Stop currently running host
            Stop(1);

            // Forward port to router to enable connecting to your local device remotely
            if (UseRouterPortForwarding)
            {
                RouterPortForwarder = RouterPortForwarding.OpenPortThread(Port, RouterPortForwarding.PROTOCOL.UDP, "godot-cutulu-udp");
            }

            // Establish cancellation token
            Token = (TokenSource = new()).Token;

            (Listener ??= new(this)).Start(port);

            AcceptPackets();
        }

        /// <summary>
        /// Disconnects from host and terminates all running processes.
        /// </summary>
        public virtual void Stop(byte exitCode = 0)
        {
            RouterPortForwarder?.Terminate();
            TokenSource?.Cancel();

            Token = CancellationToken.None;
            TokenSource = null;

            if (IsListening)
            {
                Listener.Disconnect(3);
                Listener = null;

                Stopped?.Invoke(this);
            }
        }

        #endregion

        /// <summary>
        /// Accepts incoming packets.
        /// </summary>
        private async void AcceptPackets()
        {
            (bool Success, byte[] Buffer, IPEndPoint RemoteEndPoint) packet;
            var token = Token;

            while (IsListening)
            {
                if ((packet = await Listener.Receive()).Success && token.IsCancellationRequested == false)
                {
                    Received?.Invoke(packet.RemoteEndPoint, packet.Buffer);
                }

                else await Task.Delay(10);
            }
        }
    }
}