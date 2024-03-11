using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;

namespace Cutulu
{
    public class ServerConnection<R> : Marker<R> where R : Receiver
    {
        public delegate void ConnectionPacket(ServerConnection<R> connection, byte key, byte[] bytes, Method method);
        public delegate void Disconnect(ServerConnection<R> connection);
        public Protocol.Empty onSetupComplete;
        public ConnectionPacket onReceive2;
        public Disconnect onClose;
        public TcpProtocol tcp;

        protected Dictionary<uint, ServerConnection<R>> Registry => Server?.Clients;

        public ServerNetwork<R> Server;
        public IPEndPoint endPoint;

        public virtual bool Connected() => ConnectedTcp() && ConnectedUdp();
        public bool ConnectedTcp() => tcp != null && tcp.Connected;
        public bool ConnectedUdp() => endPoint != null;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Create ServerConnection
        /// </summary>
        public ServerConnection(ref TcpClient client, uint uuid, ServerNetwork<R> server, R receiver, Protocol.Packet onReceive = null, Protocol.Empty onDisconnect = null) : base(uuid, (ushort)uuid, receiver, onReceive, onDisconnect)
        {
            this.onReceive = onReceive;
            this.Server = server;
            endPoint = null;

            $"+++ Connected [{UUID}]".Log();

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
        public void Connect(IPEndPoint udpSource)
        {
            if (udpSource == null) return;

            endPoint = udpSource;
            Send(255, SafetyId, Method.Tcp);

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
        public virtual void Send<T>(byte key, T value, Method method)
        {
            switch (method)
            {
                case Method.Tcp:
                    if (ConnectedTcp())
                    {
                        tcp.Send(key, value);
                    }
                    break;

                case Method.Udp:
                    if (ConnectedUdp())
                    {
                        Server.globalUdp.Send(key, value, endPoint);
                    }
                    break;

                default:
                    break;
            }
        }
        #endregion

        #region Connection End  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Triggered when connection is closed
        /// </summary>
        protected override void Disconnected()
        {
            $"--- Disconnected [{UUID}]".Log();
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
            if (endPoint != null && Server.Endpoints.ContainsKey(endPoint))
                lock (Server.Endpoints)
                {
                    Server.Endpoints.Remove(endPoint);
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