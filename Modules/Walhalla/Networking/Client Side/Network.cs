using Cutulu;

namespace Walhalla.Client
{
    public class Network<T> : Pointer<T> where T : Target
    {
        public TcpHandler Tcp;
        public UdpHandler Udp;

        public bool Connected => Tcp != null && Tcp.Connected;

        public Network(string tcpHost, int tcpPort, string udpHost, int udpPort, T target = null) : base(0, target)
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

        public virtual void Send<V>(byte key, V value, Method method = Method.Tcp)
        {
            try
            {
                switch (method)
                {
                    case Method.Tcp:
                        Tcp.send(key, value);
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
            Tcp = new TcpHandler(tcpHost, tcpPort, _receive, _disconnect);
            Udp = new UdpHandler(udpHost, udpPort, _receive);
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