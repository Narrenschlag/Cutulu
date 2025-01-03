namespace Cutulu.Network
{
    using System.Threading.Tasks;
    using System.Net;
    using System;

    using Protocols;
    using Core;

    public partial class Connection
    {
        public Sockets.TcpSocket Socket { get; private set; }
        public IPEndPoint EndPoint { get; private set; }
        public Host Host { get; private set; }

        public long UID { get; private set; }

        public bool IsConnected => Socket != null && Socket.IsConnected;

        public Action<short, byte[]> Received;

        public Connection(long uid, Host host, Sockets.TcpSocket socket, IPEndPoint endpoint)
        {
            EndPoint = endpoint;
            Socket = socket;

            Host = host;
            UID = uid;
        }

        /// <summary>
        /// Kicks/Cancels connection from host side.
        /// </summary>
        public virtual bool Kick()
        {
            try
            {
                Socket.Close();
                return true;
            }

            catch (Exception ex)
            {
                Debug.LogR($"[color=indianred]Failed to kick connection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends data to client.
        /// </summary>
        public virtual void Send(short key, object obj, bool reliable = true)
        {
            if (IsConnected)
            {
                var packet = PacketProtocol.Pack(key, obj, out var length);

                if (reliable) Socket?.Send(length.Encode(), packet);
                else Host.UdpHost.Listener?.Send(new[] { EndPoint }, packet);
            }
        }

        /// <summary>
        /// Sends data to client async.
        /// </summary>
        public virtual async Task SendAsync(short key, object obj, bool reliable = true)
        {
            if (IsConnected)
            {
                var packet = PacketProtocol.Pack(key, obj, out var length);

                if (reliable) await Socket?.SendAsync(length.Encode(), packet);
                else await Host.UdpHost.Listener?.SendAsync(new[] { EndPoint }, packet);
            }
        }

        /// <summary>
        /// Receive event, called by client.
        /// </summary>
        public virtual void Receive(byte[] buffer)
        {
            if (PacketProtocol.Unpack(buffer, out var key, out var unpackedBuffer))
            {
                lock (Host) Host.Receive(this, key, unpackedBuffer);

                lock (this) Received?.Invoke(key, buffer);
                lock (Host) Host.Received?.Invoke(this, key, unpackedBuffer);
            }
        }
    }
}