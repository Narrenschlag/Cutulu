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

        public ServerConnection(ref TcpClient client, uint uuid, ref Dictionary<uint, ServerConnection<D>> registry, ServerNetwork<D> server, D destination, Protocol.Packet onReceive = null, Protocol.Empty onDisconnect = null) : base(uuid, destination, onReceive, onDisconnect)
        {
            Registry = registry;

            this.onReceive = onReceive;
            this.server = server;
            endPoint = null;

            $"+++ Connected [{UUID}]".Log();

            tcp = new TcpProtocol(ref client, uuid, Receive, Disconnected);
        }

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

        public virtual bool Connected() => ConnectedTcp() && ConnectedUdp();
        public bool ConnectedTcp() => tcp != null && tcp.Connected;
        public bool ConnectedUdp() => endPoint != null;

        public ServerNetwork<D> server;
        public IPEndPoint endPoint;

        public void Connect(IPEndPoint udpSource)
        {
            if (udpSource == null) return;

            endPoint = udpSource;
            Send(0, (byte)255, Method.Tcp);

            $"Client({UUID}) has been fully connected successfully.".Log();
        }
    }
}