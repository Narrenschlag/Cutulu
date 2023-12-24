using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;

namespace Walhalla.Server
{
    public class TcpServer
    {
        public Dictionary<uint, ClientBase> Clients;
        public int TcpPort;

        public bool AcceptNewClients;

        protected TcpListener TcpListener;
        protected uint LastUID;

        /// <summary> Amount of clients currently connected to the server </summary>
        public uint ClientCount => Clients != null ? (uint)Clients.Count : 0;

        /// <summary> Simple server that handles tcp only </summary>
        public TcpServer(int port = 5000, bool accept = true)
        {
            Clients = new Dictionary<uint, ClientBase>();
            AcceptNewClients = true;
            TcpPort = port;
            LastUID = 0;

            TcpListener = new TcpListener(IPAddress.Any, port);
            TcpListener.Start(10);

            // Async client accept
            if (accept) auth();
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
            System.Net.Sockets.TcpClient tcp = await TcpListener.AcceptTcpClientAsync();

            // Register client
            lock (Clients)
            {
                ClientBase @base = newClient(ref tcp, LastUID++);
                if (@base != null) Clients.Add(@base.UID, @base);
            }

            // Welcome other clients
            auth();
        }

        /// <summary> Creates new tcp-only client </summary>
        protected virtual ClientBase newClient(ref System.Net.Sockets.TcpClient tcp, uint uid)
        {
            return new TcpClient(ref tcp, uid, ref Clients);
        }

        #region Broadcasting
        /// <summary> Broadcast to all clients </summary>
        public virtual void Broadcast<T>(byte key, T value, Method method) => Broadcast(key, value, method, Clients != null ? Clients.Values : null);

        /// <summary> Broadcast to selected clients </summary>
        public virtual void Broadcast<T>(byte key, T value, Method method, ICollection<ClientBase> receivers)
        {
            if (receivers == null || receivers.Count < 1) return;

            foreach (ClientBase client in receivers)
            {
                try { client.send(key, value, method); }
                catch (Exception ex) { throw new Exception($"[tcpServer]: Client {client.UID} was not reachable:\n{ex.Message}"); }
            }
        }

        /// <summary> Broadcast to all clients </summary>
        public virtual void Broadcast(byte key, BufferType type, byte[] bytes, Method method, bool small = true) => Broadcast(key, type, bytes, method, Clients != null ? Clients.Values : null, small);

        /// <summary> Broadcast to selected clients </summary>
        public virtual void Broadcast(byte key, BufferType type, byte[] bytes, Method method, ICollection<ClientBase> receivers, bool small = true)
        {
            if (receivers == null || receivers.Count < 1) return;

            foreach (ClientBase client in receivers)
            {
                try { client.send(key, type, bytes, method, small); }
                catch (Exception ex) { throw new Exception($"[tcpServer]: Client {client.UID} was not reachable:\n{ex.Message}"); }
            }
        }
        #endregion
    }

    public class TcpClient : ClientBase
    {
        public TcpHandler tcp;

        public delegate void PacketReceiveBy(byte key, BufferType type, byte[] bytes);

        public TcpClient(ref System.Net.Sockets.TcpClient client, uint uid, ref Dictionary<uint, ClientBase> registry, Delegates.Packet onReceive = null) : base(uid, ref registry, onReceive)
        {
            tcp = new TcpHandler(ref client, uid, _receive, _disconnect);
        }

        public bool ConnectedTcp => tcp != null && tcp.Connected;
        public virtual bool Connected => ConnectedTcp;

        public override void send<T>(byte key, T value, Method method, bool small = true)
        {
            tcp.client.NoDelay = small;

            base.send(key, value, method, small);

            if (method == Method.Tcp && ConnectedTcp) this.tcp.send(key, value, small);
        }

        public override void send(byte key, BufferType type, byte[] bytes, Method method, bool small = true)
        {
            tcp.client.NoDelay = small;

            base.send(key, type, bytes, method);

            if (method == Method.Tcp && ConnectedTcp) this.tcp.send(key, type, bytes);
        }
    }
}