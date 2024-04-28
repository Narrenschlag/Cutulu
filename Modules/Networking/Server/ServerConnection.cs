using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;

namespace Cutulu
{
    public class ServerConnection<R> : Peer<R> where R : Receiver
    {
        public delegate void ConnectionPacket(ServerConnection<R> connection, ref NetworkPackage package);
        public delegate void Disconnect(ServerConnection<R> connection);
        public Protocol.Empty onSetupComplete;
        public ConnectionPacket onReceive2;
        public Disconnect onClose;
        public TcpProtocol tcp;

        protected Dictionary<uint, ServerConnection<R>> Registry => Server?.Clients;

        public IPEndPoint RemoteTcpEndPoint { get; private set; }
        public IPEndPoint RemoteUdpEndPoint { get; private set; }
        public readonly ServerNetwork<R> Server;

        public virtual bool Connected() => ConnectedTcp() && ConnectedUdp();
        public bool ConnectedTcp() => tcp != null && tcp.Connected;
        public bool ConnectedUdp() => RemoteUdpEndPoint != null;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Create ServerConnection
        /// </summary>
        public ServerConnection(ref TcpClient client, uint uuid, ServerNetwork<R> server, R receiver, Protocol.Packet onReceive = null, Protocol.Empty onDisconnect = null) : base(uuid, (ushort)uuid, receiver, onReceive, onDisconnect)
        {
            OnReceive = onReceive;
            Server = server;

            RemoteTcpEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            RemoteUdpEndPoint = null;

            Debug.Log($"+++[{UUID}]");

            tcp = new TcpProtocol(ref client, uuid, Receive, Disconnected);
        }

        /// <summary> 
        /// (Experimental!) Transform ClientNetwork to ServerConnection
        /// </summary>
        [Obsolete("This constructor is experimental for Server2Server bonding")]
        public ServerConnection(ClientNetwork<R> client) : this(ref client.Tcp.client, client.UUID, null, null)
        {

        }

        /// <summary> 
        /// Finish setup by assigning udp endpoint
        /// </summary>
        public void ConnectToClientUdp(IPEndPoint udpSource)
        {
            if (udpSource == null) return;

            RemoteUdpEndPoint = udpSource;

            Send(255, SafetyId, Method.Tcp);
            Server.Endpoints.Set(RemoteUdpEndPoint, this);

            // Connection has been setup completely
            onSetupComplete?.Invoke();

            OnSetupComplete();
        }

        /// <summary> 
        /// Triggered when setup is complete
        /// </summary>
        protected virtual void OnSetupComplete()
        {

        }
        #endregion

        #region Send Data       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Sends data to connection endpoint
        /// </summary>
        public virtual void Send<T>(short key, T value, Method method)
        {
            switch (method)
            {
                case Method.Tcp:
                    if (ConnectedTcp())
                    {
                        tcp.Send(ref key, value);
                    }
                    break;

                case Method.Udp:
                    if (ConnectedUdp())
                    {
                        Server.globalUdp.Send(ref key, value, RemoteUdpEndPoint);
                    }
                    break;

                default:
                    break;
            }
        }

        public virtual void Send(ref short key, ref Method method) => Send<byte[]>(key, null, method);
        #endregion

        #region Receive Data    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Distributes all the incomming traffic to all registered receivers
        /// </summary>
        public override void Receive(ref NetworkPackage package)
        {
            // Premature packages
            switch (package.Key)
            {
                case 11111:
                    if (ConnectedUdp() == false && package.TryBuffer(out int udpPort))
                    {
                        ConnectToClientUdp(new(RemoteTcpEndPoint.Address, udpPort));
                        return;
                    }
                    break;

                default: break;
            }

            // Handle underlaying base
            base.Receive(ref package);

            // Invoke alternative receive callback
            if (onReceive2 != null)
            {
                lock (onReceive2)
                {
                    onReceive2?.Invoke(this, ref package);
                }
            }
        }
        #endregion

        #region Connection End  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Triggered when connection is closed
        /// </summary>
        protected override void Disconnected()
        {
            Debug.Log($"---[{UUID}]");
            base.Disconnected();

            onClose?.Invoke(this);

            if (Registry != null)
            {
                lock (Registry)
                {
                    Registry?.Remove(UUID);
                }
            }

            // Remove from endpoints
            if (RemoteUdpEndPoint != null && Server.Endpoints.ContainsKey(RemoteUdpEndPoint))
                lock (Server.Endpoints)
                {
                    Server.Endpoints.Remove(RemoteUdpEndPoint);
                }

            // Message server of disconnection
            Server?.OnClientQuit(this);
        }

        /// <summary>
        /// Closes connection to client
        /// </summary>
        public virtual void Close()
        {
            tcp?.Close();

            Disconnected();
        }
        #endregion
    }
}