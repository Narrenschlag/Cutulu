namespace Cutulu.Network
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Threading;
    using System.Net;
    using System.IO;
    using System;

    using Cutulu.Core;
    using Cutulu.Network.Protocols;

    public partial class HostConnection
    {
        public Sockets.TcpSocket Socket { get; private set; }
        public IPEndPoint EndPoint { get; private set; }
        public HostManager Host { get; private set; }

        public bool IsConnected => Socket != null && Socket.IsConnected;

        public Action<short, byte[]> Received;

        public HostConnection(HostManager host, Sockets.TcpSocket socket, IPEndPoint endpoint)
        {
            EndPoint = endpoint;
            Socket = socket;

            Host = host;
        }

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

        public virtual void Send(short key, object obj, bool reliable = true)
        {
            if (IsConnected)
            {
                var packet = PacketProtocol.Pack(key, obj, out var length);

                if (reliable) Socket.Send(length.Encode(), packet);
                else Host.UdpHost.Send(new[] { EndPoint }, packet);
            }
        }

        public virtual async Task SendAsync(short key, object obj, bool reliable = true)
        {
            if (IsConnected)
            {
                var packet = PacketProtocol.Pack(key, obj, out var length);

                if (reliable) await Socket.SendAsync(length.Encode(), packet);
                else await Host.UdpHost.SendAsync(new[] { EndPoint }, packet);
            }
        }

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