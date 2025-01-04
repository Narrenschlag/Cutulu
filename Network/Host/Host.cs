namespace Cutulu.Network
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net;
    using System;

    using Sockets;
    using Core;

    public partial class Host
    {
        public readonly Dictionary<IPEndPoint, Connection> ConnectionsByUdp = new();
        public readonly Dictionary<TcpSocket, Connection> Connections = new();

        public readonly TcpHost TcpHost;
        public readonly UdpHost UdpHost;

        public byte[] PingBuffer { get; set; }
        private long LastUID { get; set; }

        public int TcpPort { get; set; }
        public int UdpPort { get; set; }

        public bool IsListening => TcpHost?.IsListening ?? false;

        public Action<Connection, short, byte[]> Received;
        public Action<Connection> Connected, Disconnected;
        public Action Started, Stopped;

        public Host(int tcpPort, int udpPort)
        {
            TcpPort = tcpPort;
            UdpPort = udpPort;

            TcpHost = new()
            {
                Started = StartEvent,
                Stopped = StoppedEvent,

                Connected = ConnectEvent,
                Disconnected = DisconnectEvent,
            };

            UdpHost = new()
            {
                Received = UdpReceiveEvent,
            };
        }

        #region Callable Functions

        /// <summary>
        /// Starts host.
        /// </summary>
        public virtual async Task Start()
        {
            await Stop();

            ConnectionsByUdp.Clear();
            Connections.Clear();

            TcpHost.Start(TcpPort);
            UdpHost.Start(UdpPort);
        }

        /// <summary>
        /// Stops host.
        /// </summary>
        public virtual async Task Stop()
        {
            TcpHost.Stop();
            UdpHost.Stop();

            ConnectionsByUdp.Clear();
            Connections.Clear();

            LastUID = 0;

            while (TcpHost.IsListening || UdpHost.IsListening) await Task.Delay(1);
            await Task.Delay(1);
        }

        /// <summary>
        /// Sends data to connections.
        /// </summary>
        public virtual void Send(short key, object obj, params Connection[] connections) => Send(key, obj, true, connections);

        /// <summary>
        /// Sends data to connections.
        /// </summary>
        public virtual void Send(short key, object obj, bool reliable, params Connection[] connections)
        {
            if (connections.IsEmpty())
            {
                connections = Connections.Values.ToArray();
            }

            for (int i = 0; i < connections.Length; i++)
            {
                connections[i]?.Send(key, obj, reliable);
            }
        }

        /// <summary>
        /// Sends data to connections async.
        /// </summary>
        public virtual async Task SendAsync(short key, object obj, params Connection[] connections) => await SendAsync(key, obj, true, connections);

        /// <summary>
        /// Sends data to connections async.
        /// </summary>
        public virtual async Task SendAsync(short key, object obj, bool reliable, params Connection[] connections)
        {
            if (connections.IsEmpty())
            {
                connections = Connections.Values.ToArray();
            }

            for (int i = 0; i < connections.Length; i++)
            {
                await connections[i]?.SendAsync(key, obj, reliable);
            }
        }

        /// <summary>
        /// Receive event, called by connections.
        /// </summary>
        public virtual void Receive(Connection connection, short key, byte[] buffer) { }

        #endregion

        #region Event Handlers

        private void StartEvent(TcpHost host)
        {
            lock (this) Started?.Invoke();
        }

        private void StoppedEvent(TcpHost host)
        {
            lock (this) Stopped?.Invoke();
        }

        private async void ConnectEvent(TcpSocket socket)
        {
            var packet = await socket.Receive(1);

            if (packet.Success == false) return;

            switch ((ConnectionTypeEnum)packet.Buffer[0])
            {
                case ConnectionTypeEnum.Ping when PingBuffer.NotEmpty():
                    await socket.SendAsync(PingBuffer.Length.Encode(), PingBuffer);
                    return;

                // To connect, write a byte, containing the ConnectionType value and four bytes containing the port number as Int32.
                case ConnectionTypeEnum.Connect when socket.Socket.Available >= 4:
                    break;

                default:
                    return;
            }

            packet = await socket.Receive(4);
            if (packet.Success == false) return;

            await socket.SendAsync(true.Encode());

            var connection = new Connection(LastUID++, this, socket, new(((IPEndPoint)socket.Socket.RemoteEndPoint).Address, packet.Buffer.Decode<int>()));
            await socket.SendAsync(connection.UID.Encode());

            // Remove already connected connections with same address
            if (ConnectionsByUdp.TryGetValue(connection.EndPoint, out var existingConnection))
                DisconnectEvent(existingConnection.Socket);

            ConnectionsByUdp[connection.EndPoint] = connection;
            Connections[socket] = connection;

            var clear = await socket.Receive(socket.Socket.Available);

            lock (this) Connected?.Invoke(connection);

            while (active())
            {
                packet = await connection.Socket.Receive(4);
                if (packet.Success == false) continue;
                if (active() == false) continue;

                packet = await connection.Socket.Receive(packet.Buffer.Decode<int>());
                if (active() == false) continue;

                if (packet.Success) connection.Receive(packet.Buffer);
            }

            bool active() => connection != null && connection.Socket != null && connection.Socket.IsConnected;

            // Close connection
            if (connection.Kick() == false)
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

            connection.Receive(buffer);
        }

        #endregion
    }
}