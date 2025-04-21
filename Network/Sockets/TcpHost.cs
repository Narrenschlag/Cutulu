namespace Cutulu.Network.Sockets
{
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
    using System.Net;
    using System;

    using Core;

    public partial class TcpHost
    {
        public readonly Dictionary<long, TcpSocket> Sockets = new();

        public TcpListener Listener { get; private set; }
        public int Port { get; private set; }

        public bool IsListening => Socket != null && Listener.Server.IsBound;
        public Socket Socket => Listener?.Server;

        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        public bool UseRouterPortForwarding { get; set; }
        private RouterPortForwarding RouterPortForwarder { get; set; }

        public Action<TcpHost> Started, Stopped;
        public Action<TcpSocket> Connected, Disconnected;

        private long lastUID;

        /// <summary>
        /// Constructs simple tcp listener capable of IPv4 and IPv6.
        /// </summary>
        public TcpHost() { }

        #region Callable Functions

        public virtual void Start(int port)
        {
            // Stop currently running host
            Stop(1);

            // Forward port to router to enable connecting to your local device remotely
            if (UseRouterPortForwarding)
            {
                RouterPortForwarder = RouterPortForwarding.OpenPortThread(Port, RouterPortForwarding.PROTOCOL.TCP, "godot-cutulu-tcp");
            }

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
            RouterPortForwarder?.Terminate();
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

        #endregion

        #region Event Handlers

        /// <summary>
        /// Accepts incoming clients.
        /// </summary>
        private async void AcceptClients()
        {
            var _token = Token;

            while (IsListening && _token.IsCancellationRequested == false)
            {
                TcpClient client;

                try
                {
                    client = await Listener.AcceptTcpClientAsync(_token);
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

                SocketConnectEvent(client);
            }
        }

        /// <summary>
        /// Called when a client is connected.
        /// </summary>
        public virtual void SocketConnectEvent(TcpClient client)
        {
            if (client == null) return;

            lock (this)
            {
                var uid = lastUID++;

                var socket = new TcpSocket(client, this) { UID = uid };
                Sockets[uid] = socket;

                Connected?.Invoke(socket);
            }
        }

        /// <summary>
        /// Called when a client disconnects.
        /// </summary>
        public virtual void SocketDisconnectEvent(TcpSocket socket)
        {
            lock (this)
            {
                if (socket != null && Sockets.ContainsKey(socket.UID))
                {
                    Sockets.Remove(socket.UID);

                    Disconnected?.Invoke(socket);
                }
            }
        }

        #endregion
    }
}