using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;

namespace Cutulu
{
    /// <summary> 
    /// Server that handles incomming connections via TCP/UDP protocol
    /// </summary>
    public class ServerNetwork<R> where R : Receiver
    {
        public readonly Dictionary<uint, ServerConnection<R>> Clients;
        public readonly int ListentingPortTcp;
        public bool AcceptNewClients;

        protected CancellationTokenSource CancelSource;
        protected CancellationToken Cancel;

        public readonly TcpListener TcpListener;
        protected readonly R WelcomeTarget;
        protected uint LastUID;

        public readonly IPListenEnum IPType;

        /// <summary> 
        /// Amount of clients currently connected to the server 
        /// </summary>
        public uint ConnectionCount() => Clients != null ? (uint)Clients.Count : 0;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ServerNetwork(int tcpPort = 5000, int udpPort = 5001, R welcomeTarget = null, bool acceptClients = true, int maxConnectionsPerTick = 32, IPListenEnum listenTo = IPListenEnum.Any)
        {
            Endpoints = new();
            Clients = new();

            // Create a CancellationTokenSource to generate CancellationToken
            CancelSource = new();
            Cancel = CancelSource.Token;

            WelcomeTarget = welcomeTarget;
            ListentingPortTcp = tcpPort;
            AcceptNewClients = true;
            UdpPort = udpPort;
            LastUID = 0;

            $"Server started. tcp-{tcpPort} udp-{udpPort}".Log();
            globalUdp = new UdpProtocol(udpPort, ReceiveUdp);

            IPType = listenTo;
            var listen = listenTo switch
            {
                IPListenEnum.ExclusiveIPv4 => IPAddress.Any,
                _ => IPAddress.IPv6Any
            };

            TcpListener = new TcpListener(listen, tcpPort);

            if (listenTo != IPListenEnum.ExclusiveIPv4) // Enable exclusive listen to IPv6 if wished, else also listen to IPv4
                TcpListener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, listenTo == IPListenEnum.ExclusiveIPv6);

            TcpListener.Start(maxConnectionsPerTick);

            // Async client accept
            if (acceptClients) Auth();
        }

        /// <summary>
        /// Handles all incomming connections and assigns them to an id<br/>
        /// Then it starts listening to them
        /// </summary>
        protected virtual async void Auth()
        {
            // Cancellation is requested
            if (Cancel.IsCancellationRequested)
            {
                return;
            }

            // Ignore new connections
            if (!AcceptNewClients)
            {
                await Task.Delay(100);
                Auth();
            }

            // If a connection exists, the server will accept it
            TcpClient tcp = await TcpListener.AcceptTcpClientAsync(Cancel);

            // Register client
            lock (Clients)
            {
                ServerConnection<R> @base = NewClient(ref tcp, LastUID++);
                if (@base != null) Clients.Add(@base.UUID, @base);
            }

            // Welcome other clients
            Auth();
        }

        /// <summary> 
        /// Creates new tcp/udp client 
        /// </summary>
        protected virtual ServerConnection<R> NewClient(ref TcpClient tcp, uint uid)
        {
            ServerConnection<R> client;

            if (tcp.Client != null && tcp.Client.RemoteEndPoint != null)
            {
                if (tcp.Client.RemoteEndPoint is IPEndPoint endpoint)
                {
                    IPAddress address = endpoint.Address;

                    if (address != null)
                    {
                        client = new(ref tcp, uid, this, WelcomeTarget);
                    }

                    else return error($"Client endpoint address is invalid", ref tcp);
                }

                else return error($"Client had not enpoint to fetch\nRemote Tcp Endpoint valid: {tcp.Client.RemoteEndPoint != null}", ref tcp);
            }

            else return error($"Client had problems setting up the udp connection:\nTcp valid: {tcp != null}\nTcp Client instance valid: {tcp != null && tcp.Client != null}", ref tcp);

            OnClientJoin(client);
            return client;

            static ServerConnection<R> error(string message, ref TcpClient tcp)
            {
                message.LogError();
                tcp?.Close();
                return null;
            }
        }
        #endregion

        #region Broadcasting    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> Broadcast to all clients </summary>
        public virtual void Broadcast<T>(short key, T value, Method method) => Broadcast(key, value, method, Clients?.Values);

        /// <summary> Broadcast to selected clients </summary>
        public virtual void Broadcast<T>(short key, T value, Method method, ICollection<ServerConnection<R>> receivers)
        {
            if (receivers == null || receivers.Count < 1) return;

            foreach (ServerConnection<R> client in receivers)
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
        public Dictionary<IPEndPoint, ServerConnection<R>> Endpoints;
        public UdpProtocol globalUdp;
        public int UdpPort;

        private void ReceiveUdp(ref NetworkPackage package, IPEndPoint senderEndpoint, ushort safetyId)
        {
            lock (this)
            {
                // Try find existing linked connection
                if (Endpoints.TryGetValue(senderEndpoint, out ServerConnection<R> client))
                {
                    // Validate safety id and send accept package
                    if (client?.SafetyId == safetyId) client.Receive(ref package);
                }
            }
        }
        #endregion

        #region Global Events   ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected virtual void OnClientJoin(ServerConnection<R> client) { }
        public virtual void OnClientQuit(ServerConnection<R> client) { }
        #endregion

        #region Closing         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Stop()
        {
            // Cancel async operations
            Debug.Log($">_ Cancelling all async server operations... Potentially throwing irrelevant error.");
            CancelSource?.Cancel();
            CancelSource = null;

            // Close client connections
            if (Clients.NotEmpty())
            {
                foreach (var client in Clients.Values)
                {
                    client?.Close();
                }
            }
            Clients?.Clear();

            // Close listeners
            TcpListener?.Stop();
            globalUdp?.Close();
        }
        #endregion

        public enum IPListenEnum
        {
            Any,
            ExclusiveIPv4,
            ExclusiveIPv6
        }
    }
}