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

    public partial class HostConnection
    {
        public Sockets.TcpSocket Socket { get; private set; }
        public IPEndPoint EndPoint { get; private set; }

        public Action<short, byte[]> Received;

        public HostConnection(Sockets.TcpSocket socket, IPEndPoint endpoint)
        {
            EndPoint = endpoint;
            Socket = socket;
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
    }
}