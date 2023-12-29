using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Cutulu
{
    public class ServerConnection<D> where D : Destination
    {
        public delegate void Disconnect(ServerConnection<D> client);
        public TcpProtocol tcp;

        public Protocol.Packet onReceive;
        public Disconnect onClose;

        protected Dictionary<uint, ServerConnection<D>> Registry;
        public uint UID;

        public ServerConnection(ref TcpClient client, uint uid, ref Dictionary<uint, ServerConnection<D>> registry, ServerNetwork<D> server, Protocol.Packet onReceive)
        {
            Registry = registry;
            UID = uid;

            this.onReceive = onReceive;
            this.server = server;
            endPoint = null;

            $"+++ Connected [{UID}]".Log();

            tcp = new TcpProtocol(ref client, uid, _receive, _disconnect);
        }

        public virtual void send<T>(byte key, T value, Method method, bool small = true)
        {
            tcp.client.NoDelay = small;

            switch (method)
            {
                case Method.Tcp:
                    if (ConnectedTcp())
                    {
                        tcp.send(key, value, small);
                    }
                    break;

                case Method.Udp:
                    if (ConnectedUdp())
                    {
                        server.globalUdp.send(key, value, endPoint);
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary> Handles incomming traffic </summary>
        public virtual void _receive(byte key, BufferType type, byte[] bytes, Method method)
        {
            //$"{UID}> {(tcp ? "tcp" : "udp")}-package: {key} ({type}, {(bytes == null ? 0 : bytes.Length)})".Log();
            if (onReceive != null) onReceive(key, type, bytes, method);
        }

        public virtual void _disconnect()
        {
            $"--- Disconnected [{UID}]".Log();

            if (onClose != null)
                onClose(this);

            lock (Registry)
            {
                Registry.Remove(UID);
            }

            // Remove from endpoints
            lock (server.Endpoints)
            {
                if (endPoint != null && server.Endpoints.ContainsKey(endPoint))
                    server.Endpoints.Remove(endPoint);
            }

            // Message server of disconnection
            if (server != null)
            {
                server.onClientQuit(this);
            }
        }

        public virtual bool Connected() => ConnectedTcp() && ConnectedUdp();
        public bool ConnectedTcp() => tcp != null && tcp.Connected();
        public bool ConnectedUdp() => endPoint != null;

        public ServerNetwork<D> server;
        public IPEndPoint endPoint;

        public void connect(IPEndPoint udpSource)
        {
            if (udpSource == null) return;
            endPoint = udpSource;
        }
    }
}