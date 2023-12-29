namespace Cutulu
{
    public class ClientNetwork<T> : Marker<T> where T : Destination
    {
        public TcpProtocol Tcp;
        public UdpProtocol Udp;

        public bool Connected => Tcp != null && Tcp.Connected();

        public ClientNetwork(string tcpHost, int tcpPort, string udpHost, int udpPort, T destination = null) : base(0, destination)
        {
            try { _connect(tcpHost, tcpPort, udpHost, udpPort); }
            catch { $"Failed to connect to host".LogError(); }
        }

        public virtual void Send<V>(byte key, V value, Method method = Method.Tcp, bool small = true)
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