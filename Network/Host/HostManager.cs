namespace Cutulu.Network
{
    using System.Collections.Generic;
    using System.Net;
    using System;

    using Protocols;
    using Sockets;
    using Core;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public partial class HostManager
    {
        public readonly Dictionary<IPEndPoint, HostConnection> ConnectionsByUdp = new();
        public readonly Dictionary<TcpSocket, HostConnection> Connections = new();

        public readonly Sockets.TcpHost TcpHost;
        public readonly Sockets.UdpHost UdpHost;

        public int TcpPort { get; set; }
        public int UdpPort { get; set; }

        public Action<HostConnection, short, byte[]> Received;
        public Action<HostConnection> Joined, Left;
        public Action Started, Stopped;

        public HostManager(int tcpPort, int udpPort)
        {
            TcpPort = tcpPort;
            UdpPort = udpPort;

            TcpHost = new()
            {
                Started = StartedEvent,
                Stopped = StoppedEvent,

                Joined = Join,
                Left = Leave,
            };

            UdpHost = new()
            {
                Received = ReceivedUdp,
            };
        }

        public virtual async Task Start()
        {
            await Stop();

            ConnectionsByUdp.Clear();
            Connections.Clear();

            TcpHost.Start(TcpPort);
            UdpHost.Start(UdpPort);
        }

        public virtual async Task Stop()
        {
            TcpHost.Stop();
            UdpHost.Stop();

            ConnectionsByUdp.Clear();
            Connections.Clear();

            while (TcpHost.IsListening || UdpHost.IsListening) await Task.Delay(1);
            await Task.Delay(1);
        }

        private void StartedEvent(TcpHost host)
        {
            lock (this) Started?.Invoke();
        }

        private void StoppedEvent(TcpHost host)
        {
            lock (this) Stopped?.Invoke();
        }

        private void ReceivedUdp(IPEndPoint ip, byte[] buffer)
        {
            if (ConnectionsByUdp.TryGetValue(ip, out var connection) == false) return;

            connection.Receive(buffer);
        }

        private async void Join(TcpSocket socket)
        {
            var packet = await socket.Receive(1);

            if (packet.Success == false) return;

            switch ((ConnectionTypeEnum)packet.Buffer[0])
            {
                case ConnectionTypeEnum.Ping when socket.Socket.Available == 0:
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

            var connection = new HostConnection(this, socket, new(((IPEndPoint)socket.Socket.RemoteEndPoint).Address, packet.Buffer.Decode<int>()));

            // Remove already connected connections with same address
            if (ConnectionsByUdp.TryGetValue(connection.EndPoint, out var existingConnection))
                Leave(existingConnection.Socket);

            Debug.LogR($"new connection: {connection.EndPoint}");
            ConnectionsByUdp[connection.EndPoint] = connection;
            Connections[socket] = connection;

            var clear = await socket.Receive(socket.Socket.Available);

            lock (this) Joined?.Invoke(connection);

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

        private void Leave(TcpSocket socket)
        {
            if (Connections.TryGetValue(socket, out var connection))
            {
                ConnectionsByUdp.TryRemove(connection.EndPoint);
                Connections.Remove(socket);

                socket.Close();

                lock (this) Left?.Invoke(connection);
            }
        }

        public virtual void Receive(HostConnection connection, short key, byte[] buffer) { }

        public virtual void SendTcp(HostConnection connection, short key, object obj)
        {
            if (connection.Socket.IsConnected == false) return;

            var packet = PacketProtocol.Pack(key, obj, out var length);

            connection.Socket.Send(length.Encode(), packet);
        }

        public virtual void SendUdp(HostConnection connection, short key, object obj)
        {
            if (connection.Socket.IsConnected == false) return;

            var packet = PacketProtocol.Pack(key, obj, out var length);

            UdpHost.Send(new[] { connection.EndPoint }, packet);
        }
    }
}