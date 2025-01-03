namespace Cutulu.Network.Sockets
{
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
    using System.Net;
    using System;
    using Cutulu.Core;

    public partial class TcpHost
    {
        public readonly Dictionary<long, TcpSocket> Sockets = new();

        public TcpListener Listener { get; private set; }
        public int Port { get; private set; }

        public bool IsListening => Socket != null && Listener.Server.IsBound;
        public Socket Socket => Listener?.Server;

        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        public Action<TcpHost> Started, Stopped;
        public Action<TcpSocket> Joined, Left;

        private long lastUID;

        /// <summary>
        /// Constructs simple tcp listener capable of IPv4 and IPv6.
        /// </summary>
        public TcpHost() { }

        public virtual void Start(int port)
        {
            // Stop currently running host
            Stop(1);

            // Establish cancellation token
            Token = (TokenSource = new()).Token;
            lastUID = 0;

            if (Listener == null)
            {
                Listener = new TcpListener(IPAddress.IPv6Any, Port = port);

                Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }

            // Establish tcp listener
            Listener.Start();

            AcceptClients();
        }

        /// <summary>
        /// Disconnects from host and terminates all running processes.
        /// </summary>
        public virtual void Stop(byte exitCode = 0)
        {
            TokenSource?.Cancel();

            Token = CancellationToken.None;
            TokenSource = null;

            Sockets.Clear();

            if (IsListening)
            {
                Listener.Stop();
                Listener = null;

                Stopped?.Invoke(this);
            }
        }

        private async void AcceptClients()
        {
            var token = Token;

            while (IsListening && token.IsCancellationRequested == false)
            {
                TcpClient client;

                try
                {
                    client = await Listener.AcceptTcpClientAsync(token);
                }

                catch (Exception ex)
                {
                    switch (ex)
                    {
                        case OperationCanceledException:
                            break;

                        default:
                            if (ex.StackTrace.Contains("CancellationToken")) break;

                            Debug.LogError($"TCPHOST_ACCEPT_ERROR({ex.GetType().Name}, {ex.Message})\n{ex.StackTrace}");
                            break;
                    }

                    break;
                }

                SocketJoined(client);
            }
        }

        public virtual void SocketJoined(TcpClient client)
        {
            if (client == null) return;

            var uid = lastUID++;

            var socket = new TcpSocket(client, this) { UID = uid };
            Sockets[uid] = socket;

            Joined?.Invoke(socket);
        }

        public virtual void SocketLeave(TcpSocket socket)
        {
            if (socket != null && Sockets.ContainsKey(socket.UID))
            {
                Sockets.Remove(socket.UID);

                Left?.Invoke(socket);
            }
        }
    }
}