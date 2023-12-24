using System.Collections.Generic;
using System.Net;

namespace Walhalla.Server
{
    public class AdvancedServer : TcpServer
    {
        public Dictionary<IPEndPoint, AdvancedClient> Endpoints;
        public Dictionary<IPAddress, AdvancedClient> Queue;
        public UdpHandler globalUdp;
        public int UdpPort;

        /// <summary> Simple server that handles tcp and udp </summary>
        public AdvancedServer(int tcpPort = 5000, int udpPort = 5001, bool accept = true) : base(tcpPort, false)
        {
            Endpoints = new Dictionary<IPEndPoint, AdvancedClient>();
            Queue = new Dictionary<IPAddress, AdvancedClient>();
            UdpPort = udpPort;

            globalUdp = new UdpHandler(udpPort, _receiveUdp);

            // Async client accept
            if (accept) auth();
        }

        /// <summary> Creates new tcp/udp client </summary>
        protected override ClientBase newClient(ref System.Net.Sockets.TcpClient tcp, uint uid)
        {
            AdvancedClient client = new AdvancedClient(ref tcp, uid, ref Clients, this);
            IPEndPoint endpoint = tcp.Client.RemoteEndPoint as IPEndPoint;

            if (endpoint != null)
            {
                IPAddress address = endpoint.Address;

                if (address != null)
                    lock (Queue)
                    {
                        if (Queue.ContainsKey(address)) Queue[address] = client;
                        else Queue.Add(address, client);
                    }
            }

            return client;
        }

        private void _receiveUdp(byte key, BufferType type, byte[] bytes, IPEndPoint endpoint)
        {
            lock (this)
            {
                // Move queued element to endpoint registry
                if (!Endpoints.TryGetValue(endpoint, out AdvancedClient client))
                {
                    if (Queue.TryGetValue(endpoint.Address, out client))
                    {
                        Endpoints.Add(endpoint, client);
                        Queue.Remove(endpoint.Address);

                        client.connect(endpoint);
                    }
                }

                if (client != null)
                {
                    client._receive(key, type, bytes, Method.Udp);
                }
            }
        }
    }

    public class AdvancedClient : TcpClient
    {
        public AdvancedServer server;
        public IPEndPoint endPoint;

        public AdvancedClient(ref System.Net.Sockets.TcpClient client, uint uid, ref Dictionary<uint, ClientBase> registry, AdvancedServer server, Delegates.Packet onReceive = null) : base(ref client, uid, ref registry, onReceive)
        {
            this.server = server;
            endPoint = null;
        }

        public void connect(IPEndPoint udpSource)
        {
            if (udpSource == null) return;
            endPoint = udpSource;
        }

        public override bool Connected => base.Connected && ConnectedUdp;
        public bool ConnectedUdp => endPoint != null;

        public override void send(byte key, BufferType type, byte[] bytes, Method method)
        {
            base.send(key, type, bytes, method);

            if (method == Method.Udp && ConnectedUdp && endPoint != null)
            {
                server.globalUdp.send(key, type, bytes, endPoint);
            }
        }

        public override void send<T>(byte key, T value, Method method, bool small = true)
        {
            base.send(key, value, method, small);

            if (method == Method.Udp && ConnectedUdp && endPoint != null)
            {
                server.globalUdp.send(key, value, endPoint);
            }
        }

        public override void _disconnect()
        {
            base._disconnect();

            // Remove from endpoints
            lock (server.Endpoints)
            {
                if (endPoint != null && server.Endpoints.ContainsKey(endPoint))
                    server.Endpoints.Remove(endPoint);
            }
        }
    }
}