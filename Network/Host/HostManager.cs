namespace Cutulu.Network
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net;
    using System;

    using Sockets;
    using Core;

    public partial class HostManager
    {
        public readonly Dictionary<byte, ConnectionHandler> ConnectionHandlers = new([
            FormatHandler(new ConnectHandler((byte)ConnectionTypeEnum.Connect)),
            FormatHandler(new PingHandler((byte)ConnectionTypeEnum.Ping)),
        ]);

        public readonly Dictionary<IPEndPoint, Connection> ConnectionsByUdp = [];
        public readonly Dictionary<TcpSocket, Connection> Connections = [];

        public readonly TcpHost TcpHost;
        public readonly UdpHost UdpHost;

        public byte[] PingBuffer { get; set; }
        private long LastUID { get; set; }

        public bool UseRouterPortForwarding { get; set; } = false;
        public int MaxClients { get; set; } = 0;
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }

        public bool IsListening => TcpHost?.IsListening ?? false;

        public Action<Connection, short, byte[]> Received;
        public Action<Connection> Connected, Disconnected;
        public Action Started, Stopped;

        private Wrapper wrapper { get; set; }
        public Wrapper GetWrapper() => wrapper ??= new(this);

        public HostManager()
        {
            TcpHost = new()
            {
                Started = StartEvent,
                Stopped = StoppedEvent,

                Connected = HandleNewClient,
                Disconnected = DisconnectEvent,
            };

            UdpHost = new()
            {
                Received = UdpReceiveEvent,
            };
        }

        public HostManager(int tcpPort, int udpPort) : this()
        {
            TcpPort = tcpPort;
            UdpPort = udpPort;
        }

        #region Validators

        public bool TryGetHandler(byte key, out ConnectionHandler validator) => ConnectionHandlers.TryGetValue(key, out validator);

        public bool ContainsHandler(byte key) => ConnectionHandlers.ContainsKey(key);

        public KeyValuePair<byte, ConnectionHandler> RegisterHandler(ConnectionHandler validator)
        {
            var val = FormatHandler(validator);
            ConnectionHandlers[validator.Key] = validator;
            return val;
        }

        private static KeyValuePair<byte, ConnectionHandler> FormatHandler(ConnectionHandler validator)
        => new(validator.Key, validator);

        #endregion

        #region Callable Functions

        /// <summary>
        /// Starts host
        /// </summary>
        public virtual async Task Start()
        {
            await Stop();

            ConnectionsByUdp.Clear();
            Connections.Clear();

            TcpHost.UseRouterPortForwarding = UseRouterPortForwarding;
            TcpHost.Start(TcpPort);

            UdpHost.UseRouterPortForwarding = UseRouterPortForwarding;
            UdpHost.Start(UdpPort);
        }

        /// <summary>
        /// Stops host
        /// </summary>
        public virtual async Task Stop()
        {
            // Kick all connections before stopping the server
            foreach (var _connection in Connections.Values) _connection?.Kick();

            TcpHost.Stop();
            UdpHost.Stop();

            ConnectionsByUdp.Clear();
            Connections.Clear();

            LastUID = 0;

            while (TcpHost.IsListening || UdpHost.IsListening) await Task.Delay(1);
            await Task.Delay(1);
        }

        /// <summary>
        /// Sends data to connections
        /// </summary>
        public virtual void Send(short key, object obj, params Connection[] connections) => Send(key, obj, true, connections);

        /// <summary>
        /// Sends data to connections
        /// </summary>
        public virtual void Send(short key, object obj, bool reliable, params Connection[] connections)
        {
            if (connections.IsEmpty())
            {
                connections = [.. Connections.Values];
            }

            for (int i = 0; i < connections.Length; i++)
            {
                connections[i]?.Send(key, obj, reliable);
            }
        }

        /// <summary>
        /// Sends data to connections async
        /// </summary>
        public virtual async Task SendAsync(short key, object obj, params Connection[] connections) => await SendAsync(key, obj, true, connections);

        /// <summary>
        /// Sends data to connections async.
        /// </summary>
        public virtual async Task SendAsync(short key, object obj, bool reliable, params Connection[] connections)
        {
            if (connections.IsEmpty())
            {
                connections = [.. Connections.Values];
            }

            for (int i = 0; i < connections.Length; i++)
            {
                await connections[i]?.SendAsync(key, obj, reliable);
            }
        }

        /// <summary>
        /// Receive event, called by connections.
        /// </summary>
        public virtual bool ReadPacket(Connection connection, short key, byte[] buffer) => false;

        #endregion

        #region Event Handlers

        protected virtual void StartEvent(TcpHost host)
        {
            lock (this) Started?.Invoke();
        }

        protected virtual void StoppedEvent(TcpHost host)
        {
            lock (this) Stopped?.Invoke();
        }

        protected virtual void ConnectedEvent(Connection _connection)
        {
            Debug.Log($"New stream connected0: {_connection.UserId}");
            lock (this) Connected?.Invoke(_connection);
        }

        private async void HandleNewClient(TcpSocket socket)
        {
            Debug.Log($"New socket connected-1: {socket.Port}");
            var packet = await socket.Receive(1);

            if (packet.Success == false)
            {
                Debug.LogError($"Failed to receive connection type length from {socket.Socket.RemoteEndPoint}. Closing connection.");
                socket.Close();
                return;
            }

            if (ConnectionHandlers.TryGetValue(packet.Buffer[0], out var handler) && handler.NotNull())
            {
                var (Status, Data) = await handler.Validate(GetWrapper(), socket);

                if (Status) await handler.Handle(GetWrapper(), socket, Data);
                else Debug.LogError($"Handler<{handler.GetType().Name}> does not approve of connection. Closing connection.");
            }

            else
            {
                Debug.LogError($"Unknown connection type({packet.Buffer[0]}) received from {socket.Socket.RemoteEndPoint}. No handler has been assigned. Closing connection.");
            }

            socket?.Close();
        }

        private void DisconnectEvent(TcpSocket socket)
        {
            if (Connections.TryGetValue(socket, out var connection))
            {
                ConnectionsByUdp.TryRemove(connection.EndPoint);
                Connections.Remove(socket);

                socket.Close();

                lock (this) Disconnected?.Invoke(connection);
            }
        }

        private void UdpReceiveEvent(IPEndPoint ip, byte[] buffer)
        {
            if (ConnectionsByUdp.TryGetValue(ip, out var connection) == false) return;

            connection.ReceiveBuffer(buffer);
        }

        #endregion

        public class Wrapper(HostManager manager)
        {
            public readonly HostManager Manager = manager;

            public void InvokeDisconnect(TcpSocket socket) => Manager.DisconnectEvent(socket);

            public void InvokeConnect(Connection connection) => Manager.ConnectedEvent(connection);

            public long NextUID() => Manager.LastUID++;
        }
    }
}