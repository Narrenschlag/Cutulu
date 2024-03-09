using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace Cutulu
{
    public class ServerConnection<D> : Marker<D> where D : Destination
    {
        public delegate void Disconnect(ServerConnection<D> client);
        public Disconnect onClose;
        public TcpProtocol tcp;

        protected Dictionary<uint, ServerConnection<D>> Registry;

        public ServerNetwork<D> server;
        public IPEndPoint endPoint;

        public virtual bool Connected() => ConnectedTcp() && ConnectedUdp();
        public bool ConnectedTcp() => tcp != null && tcp.Connected;
        public bool ConnectedUdp() => endPoint != null;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ServerConnection(ref TcpClient client, uint uuid, ref Dictionary<uint, ServerConnection<D>> registry, ServerNetwork<D> server, D destination, Protocol.Packet onReceive = null, Protocol.Empty onDisconnect = null) : base(uuid, (ushort)uuid, destination, onReceive, onDisconnect)
        {
            Registry = registry;

            this.onReceive = onReceive;
            this.server = server;
            endPoint = null;

            $"+++ Connected [{UUID}]".Log();

            tcp = new TcpProtocol(ref client, uuid, Receive, Disconnected);
        }

        /// <summary> 
        /// Finish setup by assigning udp endpoint
        /// </summary>
        public void Connect(IPEndPoint udpSource)
        {
            if (udpSource == null) return;

            endPoint = udpSource;
            Send(255, SafetyId, Method.Tcp);

            $"Client({UUID}) has been fully connected successfully.".Log();
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
                        server.globalUdp.Send(key, value, endPoint);
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

            lock (Registry)
            {
                Registry.Remove(UUID);
            }

            // Remove from endpoints
            if (endPoint != null && server.Endpoints.ContainsKey(endPoint))
                lock (server.Endpoints)
                {
                    server.Endpoints.Remove(endPoint);
                }

            // Message server of disconnection
            server?.OnClientQuit(this);
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