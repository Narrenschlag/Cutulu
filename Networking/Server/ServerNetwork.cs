using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;

namespace Cutulu
{
    public class ServerNetwork<D> where D : Destination
    {
        public Dictionary<uint, ServerConnection<D>> Clients;
        public bool AcceptNewClients;
        public int TcpPort;

        protected TcpListener TcpListener;
        protected uint LastUID;

        private D WelcomeTarget;

        /// <summary> Amount of clients currently connected to the server </summary>
        public uint ClientCount => Clients != null ? (uint)Clients.Count : 0;

        /// <summary> Simple server that handles tcp only </summary>
        public ServerNetwork(int tcpPort = 5000, int udpPort = 5001, D welcomeTarget = null, bool acceptClients = true)
        {
            Endpoints = new Dictionary<IPEndPoint, ServerConnection<D>>();
            Queue = new Dictionary<IPAddress, ServerConnection<D>>();
            Clients = new Dictionary<uint, ServerConnection<D>>();

            WelcomeTarget = welcomeTarget;
            AcceptNewClients = true;
            TcpPort = tcpPort;
            UdpPort = udpPort;
            LastUID = 0;

            $"Server started. tcp-{tcpPort} udp-{udpPort}".Log();
            globalUdp = new UdpProtocol(udpPort, _receiveUdp);

            TcpListener = new TcpListener(IPAddress.Any, tcpPort);
            TcpListener.Start(10);

            // Async client accept
            if (acceptClients) auth();
        }

        /// <summary>
        /// Handles all incomming connections and assigns them to an id<br/>
        /// Then it starts listening to them
        /// </summary>
        protected virtual async void auth()
        {
            // Ignore new connections
            if (!AcceptNewClients)
            {
                await Task.Delay(100);
                auth();
            }

            // If a connection exists, the server will accept it
            TcpClient tcp = await TcpListener.AcceptTcpClientAsync();

            // Register client
            lock (Clients)
            {
                ServerConnection<D> @base = newClient(ref tcp, LastUID++);
                if (@base != null) Clients.Add(@base.UUID, @base);
            }

            // Welcome other clients
            auth();
        }

        /// <summary> Creates new tcp/udp client </summary>
        protected virtual ServerConnection<D> newClient(ref TcpClient tcp, uint uid)
        {
            ServerConnection<D> client = new ServerConnection<D>(ref tcp, uid, ref Clients, this, WelcomeTarget);
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

            onClientJoin(client);
            return client;
        }

        #region Broadcasting
        /// <summary> Broadcast to all clients </summary>
        public virtual void Broadcast<T>(byte key, T value, Method method) => Broadcast(key, value, method, Clients != null ? Clients.Values : null);

        /// <summary> Broadcast to selected clients </summary>
        public virtual void Broadcast<T>(byte key, T value, Method method, ICollection<ServerConnection<D>> receivers)
        {
            if (receivers == null || receivers.Count < 1) return;

            foreach (ServerConnection<D> client in receivers)
            {
                try { client.Send(key, value, method); }
                catch (Exception ex) { throw new Exception($"[tcpServer]: Client {client.UUID} was not reachable:\n{ex.Message}"); }
            }
        }
        #endregion

        #region Udp
        public Dictionary<IPEndPoint, ServerConnection<D>> Endpoints;
        public Dictionary<IPAddress, ServerConnection<D>> Queue;
        public UdpProtocol globalUdp;
        public int UdpPort;

        private void _receiveUdp(byte key, BufferType type, byte[] bytes, IPEndPoint endpoint)
        {
            lock (this)
            {
                // Move queued element to endpoint registry
                if (!Endpoints.TryGetValue(endpoint, out ServerConnection<D> client))
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
        #endregion

        protected virtual void onClientJoin(ServerConnection<D> client) { }
        public virtual void onClientQuit(ServerConnection<D> client) { }
    }
}