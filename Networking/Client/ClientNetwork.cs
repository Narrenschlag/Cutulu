namespace Cutulu
{
    public class ClientNetwork<T> : Marker<T> where T : Destination
    {
        public TcpProtocol Tcp;
        public UdpProtocol Udp;

        public bool Connected => Tcp != null && Tcp.Connected();

        public ClientNetwork(string tcpHost, int tcpPort, string udpHost, int udpPort, T target = null) : base(0, target)
        {
            try { _connect(tcpHost, tcpPort, udpHost, udpPort); }
            catch { $"Failed to connect to host".LogError(); }
        }

        protected override void _receive(byte key, BufferType type, byte[] bytes, Method method)
        {
            //$"{method}-package: {key} ({type}, {(bytes == null ? 0 : bytes.Length)})".Log();
            base._receive(key, type, bytes, method);

            if (onReceive != null)
            {
                onReceive(key, type, bytes, method);
            }
        }

        public virtual void Send<V>(byte key, V value, Method method = Method.Tcp, bool small = true)
        {
            try
            {
                switch (method)
                {
                    case Method.Tcp:
                        Tcp.send(key, value, small);
                        break;

                    case Method.Udp:
                        Udp.send(key, value);
                        break;

                    default: break;
                }
            }
            catch { }
        }

        protected virtual void _connect(string tcpHost, int tcpPort, string udpHost, int udpPort)
        {
            Tcp = new TcpProtocol(tcpHost, tcpPort, _receive, _disconnect);
            Udp = new UdpProtocol(udpHost, udpPort, _receive);
        }

        protected override void _disconnect()
        {
            if (Tcp != null) Tcp.Close();
            if (Udp != null) Udp.Close();

            base._disconnect();

            "disconnected.".LogError();
        }
    }
}