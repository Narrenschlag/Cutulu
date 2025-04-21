namespace Cutulu.Network
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net;
    using System;

    using Protocols;
    using Core;

    public partial class Connection(long uid, HostManager host, Sockets.TcpSocket socket, IPEndPoint endpoint) : Tagable
    {
        public Sockets.TcpSocket Socket { get; private set; } = socket;
        public IPEndPoint EndPoint { get; private set; } = endpoint;
        public HostManager Host { get; private set; } = host;

        public readonly HashSet<Listener> Listeners = [];
        public long UserID { get; private set; } = uid;

        public bool IsConnected => Socket != null && Socket.IsConnected;
        long Tagable.GetUniqueTagID() => UserID;

        public event Action<short, byte[]> Received;

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
        public virtual void ReceiveBuffer(byte[] buffer)
        {
            if (PacketProtocol.Unpack(buffer, out var key, out var unpackedBuffer))
            {
                // First let the host read the packet
                lock (Host) if (Host.ReadPacket(this, key, unpackedBuffer)) return;

                // Host didn't consume the packet, let the listeners read it
                lock (Listeners)
                    foreach (var _listener in Listeners)
                        if ((bool)(_listener?.ReadPacket(key, unpackedBuffer))) return;

                // No one consumed the packet, let the events read it
                lock (this) Received?.Invoke(key, unpackedBuffer);
                lock (Host) Host.Received?.Invoke(this, key, unpackedBuffer);
            }
        }
    }
}