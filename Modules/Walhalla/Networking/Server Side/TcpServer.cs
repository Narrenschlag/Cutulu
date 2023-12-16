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
            TcpClient tcp = await TcpListener.AcceptTcpClientAsync();

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
        protected virtual ClientBase newClient(ref TcpClient tcp, uint uid)
        {
            return new SimpleClient(ref tcp, uid, ref Clients);
        }

        #region Broadcasting
        /// <summary> Broadcast to all clients </summary>
        public virtual void Broadcast<T>(byte key, T value, bool tcp) => Broadcast(key, value, tcp, Clients != null ? Clients.Values : null);

        /// <summary> Broadcast to selected clients </summary>
        public virtual void Broadcast<T>(byte key, T value, bool tcp, ICollection<ClientBase> receivers)
        {
            if (receivers == null || receivers.Count < 1) return;

            foreach (ClientBase client in receivers)
            {
                try { client.send(key, value, tcp); }
                catch (Exception ex) { throw new Exception($"[tcpServer]: Client {client.UID} was not reachable:\n{ex.Message}"); }
            }
        }

        /// <summary> Broadcast to all clients </summary>
        public virtual void Broadcast(byte key, BufferType type, byte[] bytes, bool tcp) => Broadcast(key, type, bytes, tcp, Clients != null ? Clients.Values : null);

        /// <summary> Broadcast to selected clients </summary>
        public virtual void Broadcast(byte key, BufferType type, byte[] bytes, bool tcp, ICollection<ClientBase> receivers)
        {
            if (receivers == null || receivers.Count < 1) return;

            foreach (ClientBase client in receivers)
            {
                try { client.send(key, type, bytes, tcp); }
                catch (Exception ex) { throw new Exception($"[tcpServer]: Client {client.UID} was not reachable:\n{ex.Message}"); }
            }
        }
        #endregion
    }

    public class SimpleClient : ClientBase
    {
        public TcpHandler tcp;

        public delegate void PacketReceiveBy(byte key, BufferType type, byte[] bytes);

        public SimpleClient(ref TcpClient client, uint uid, ref Dictionary<uint, ClientBase> registry, PacketReceive onReceive = null) : base(uid, ref registry, onReceive)
        {
            tcp = new TcpHandler(ref client, uid, receiveTcp, _disconnect);
        }

        public virtual bool Connected => ConnectedTcp;
        public bool ConnectedTcp => tcp != null && tcp.Connected;

        private void receiveTcp(byte key, BufferType type, byte[] bytes)
            => _receive(key, type, bytes, true);

        public override void send<T>(byte key, T value, bool tcp)
        {
            base.send(key, value, tcp);

            if (tcp && ConnectedTcp) this.tcp.send(key, value);
        }

        public override void send(byte key, BufferType type, byte[] bytes, bool tcp)
        {
            base.send(key, type, bytes, tcp);

            if (tcp && ConnectedTcp) this.tcp.send(key, type, bytes);
        }
    }
}