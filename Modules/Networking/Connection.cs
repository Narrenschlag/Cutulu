namespace Cutulu.Core.Networking
{
    using System.Net.Sockets;
    using System.Net;

    using System.Threading.Tasks;
    using System.Threading;

    using System.IO;
    using System;

    using Cutulu.Core;

    public partial class Connection
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        public readonly CancellationToken CancellationToken;
        private readonly Host Host;
        private bool closed;

        private readonly IPEndPoint UdpEndpoint;
        private readonly TcpClient Client;
        public readonly long UID;

        public Action<short, byte[]> ReceivedTcp, ReceivedUdp, ReceivedAny;
        public Action<Connection> ClientDisconnected;

        public bool Running => Host != null && Host.Running && closed == false;

        public Connection(ref TcpClient client, ref IPEndPoint udpEndpoint, long uid, Host server, CancellationToken cancellationToken)
        {
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = cancellationTokenSource.Token;
            closed = false;

            UdpEndpoint = udpEndpoint;
            Client = client;
            Host = server;
            UID = uid;

            Host.Connections[UID] = this;
        }

        public virtual void Close()
        {
            closed = true;

            if (Host != null)
            {
                Host.Connections.TryRemove(UID);

                if (UdpEndpoint != null)
                    Host.UdpEndpoints.TryRemove(UdpEndpoint);
            }

            Host?.ConnectionClosed?.Invoke(this);
            ClientDisconnected?.Invoke(this);

            cancellationTokenSource?.Cancel();
            Client?.Close();

            Debug.Log($"Remote client has been disconnected.");
        }

        #region Udp
        public virtual void ReceiveUdp(short key, byte[] buffer)
        {
            switch (key)
            {
                default:
                    Host?.ReceivedUdp?.Invoke(this, key, buffer);
                    Host?.ReceivedAny?.Invoke(this, key, buffer);
                    ReceivedUdp?.Invoke(key, buffer);
                    ReceivedAny?.Invoke(key, buffer);
                    break;
            }
        }

        public virtual async Task WriteUdpAsync(short key, byte[] buffer)
        {
            if (Running == false || Host.UdpHost == null || UdpEndpoint == null) return;

            using var stream = new MemoryStream();
            await stream.WriteAsync(BitConverter.GetBytes(key), CancellationToken);
            await stream.WriteAsync(buffer, CancellationToken);

            if (Running && Host != null && Host.UdpHost != null)
                await Host.UdpHost.SendAsync(stream.ToArray(), UdpEndpoint, CancellationToken);
        }

        public virtual void WriteUdp(short key, byte[] buffer)
        {
            if (Running == false || Host.UdpHost == null || UdpEndpoint == null) return;

            using var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(key));
            stream.Write(buffer);

            Host?.UdpHost?.Send(stream.ToArray(), UdpEndpoint);
        }
        #endregion

        #region Tcp
        public virtual void ReceiveTcp(short key, byte[] buffer)
        {
            switch (key)
            {
                default:
                    Host?.ReceivedTcp?.Invoke(this, key, buffer);
                    Host?.ReceivedAny?.Invoke(this, key, buffer);
                    ReceivedTcp?.Invoke(key, buffer);
                    ReceivedAny?.Invoke(key, buffer);
                    break;
            }
        }

        public virtual async Task WriteTcpAsync(short key, byte[] buffer)
        {
            if (Running == false || Client.GetStream() is not NetworkStream stream) return;

            using var memory = new MemoryStream();
            await memory.WriteAsync(BitConverter.GetBytes(buffer.Length + 2), CancellationToken);
            await memory.WriteAsync(BitConverter.GetBytes(key), CancellationToken);
            await memory.WriteAsync(buffer, CancellationToken);

            await stream.WriteAsync(memory.ToArray());
            await stream.FlushAsync(CancellationToken);
        }

        public virtual void WriteTcp(short key, byte[] buffer)
        {
            if (Running == false || Client.GetStream() is not NetworkStream stream) return;

            stream.Write(BitConverter.GetBytes(buffer.Length + 2));
            stream.Write(BitConverter.GetBytes(key));
            stream.Write(buffer);
            stream.Flush();
        }
        #endregion
    }
}