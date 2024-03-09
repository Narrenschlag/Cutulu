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

        private readonly D WelcomeTarget;

        /// <summary> 
        /// Amount of clients currently connected to the server 
        /// </summary>
        public uint ConnectionCount() => Clients != null ? (uint)Clients.Count : 0;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
            globalUdp = new UdpProtocol(udpPort, ReceiveUdp);

            TcpListener = new TcpListener(IPAddress.Any, tcpPort);
            TcpListener.Start(10);

            // Async client accept
            if (acceptClients) Auth();
        }

        /// <summary>
        /// Handles all incomming connections and assigns them to an id<br/>
        /// Then it starts listening to them
        /// </summary>
        protected virtual async void Auth()
        {
            // Ignore new connections
            if (!AcceptNewClients)
            {
                await Task.Delay(100);
                Auth();
            }

            // If a connection exists, the server will accept it
            TcpClient tcp = await TcpListener.AcceptTcpClientAsync();

            // Register client
            lock (Clients)
            {
                ServerConnection<D> @base = NewClient(ref tcp, LastUID++);
                if (@base != null) Clients.Add(@base.UUID, @base);
            }

            // Welcome other clients
            Auth();
        }

        /// <summary> 
        /// Creates new tcp/udp client 
        /// </summary>
        protected virtual ServerConnection<D> NewClient(ref TcpClient tcp, uint uid)
        {
            ServerConnection<D> client;
            if (tcp.Client != null && tcp.Client.RemoteEndPoint != null)
            {
                if (tcp.Client.RemoteEndPoint is IPEndPoint endpoint)
                {
                    IPAddress address = endpoint.Address;

                    if (address != null)
                        lock (Queue)
                        {
                            client = new(ref tcp, uid, ref Clients, this, WelcomeTarget);

                            if (Queue.ContainsKey(address)) Queue[address] = client;
                            else Queue.Add(address, client);
                        }

                    else return error($"Client endpoint address is invalid", ref tcp);
                }

                else return error($"Client had not enpoint to fetch\nRemote Tcp Endpoint valid: {tcp.Client.RemoteEndPoint != null}", ref tcp);
            }

            else return error($"Client had problems setting up the udp connection:\nTcp valid: {tcp != null}\nTcp Client instance valid: {tcp != null && tcp.Client != null}", ref tcp);

            OnClientJoin(client);
            return client;

            static ServerConnection<D> error(string message, ref TcpClient tcp)
            {
                message.LogError();
                tcp?.Close();
                return null;
            }
        }
        #endregion

        #region Broadcasting    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> Broadcast to all clients </summary>
        public virtual void Broadcast<T>(byte key, T value, Method method) => Broadcast(key, value, method, Clients?.Values);

        /// <summary> Broadcast to selected clients </summary>
        public virtual void Broadcast<T>(byte key, T value, Method method, ICollection<ServerConnection<D>> receivers)
        {
            if (receivers == null || receivers.Count < 1) return;

            foreach (ServerConnection<D> client in receivers)
            {
                if (client == null)
                {
                    continue;
                }

                try { client.Send(key, value, method); }
                catch (Exception ex) { throw new Exception($"[tcpServer]: Client {client.UUID} was not reachable:\n{ex.Message}"); }
            }
        }
        #endregion

        #region Udp             ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Dictionary<IPEndPoint, ServerConnection<D>> Endpoints;
        public Dictionary<IPAddress, ServerConnection<D>> Queue;
        public UdpProtocol globalUdp;
        public int UdpPort;

        private void ReceiveUdp(byte key, byte[] bytes, IPEndPoint endpoint, ushort safetyId)
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

                        client.Connect(endpoint);
                    }
                }

                // Validate safety id and send accept package
                if (client != null && client.SafetyId == safetyId)
                {
                    client.Receive(key, bytes, Method.Udp);
                }
            }
        }
        #endregion

        #region Global Events   ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected virtual void OnClientJoin(ServerConnection<D> client) { }
        public virtual void OnClientQuit(ServerConnection<D> client) { }
        #endregion

        #region Closing         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Close()
        {
            TcpListener?.Stop();

            Clients = null;
        }
        #endregion
    }
}