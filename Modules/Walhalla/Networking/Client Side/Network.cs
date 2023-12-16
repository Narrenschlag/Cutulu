using Cutulu;

namespace Walhalla.Client
{
    public class Network
    {
        public TcpHandler Tcp;
        public UdpHandler Udp;

        public delegate void Packet(byte key, BufferType type, byte[] data, bool tcp);
        public delegate void Empty();

        public static Empty onDisconnect;
        public static Packet onReceive;

        public bool Connected => Tcp != null && Tcp.Connected;

        public Network(string tcpHost, int tcpPort, string udpHost, int udpPort)
        {
            try { _connect(tcpHost, tcpPort, udpHost, udpPort); }
            catch { $"Failed to connect to host".LogError(); }
        }

        private void _receiveUdp(byte key, BufferType type, byte[] data) => Receive(key, type, data, false);
        private void _receiveTcp(byte key, BufferType type, byte[] data) => Receive(key, type, data, true);
        public virtual void Receive(byte key, BufferType type, byte[] bytes, bool tcp)
        {
            //$"{(tcp ? "tcp" : "udp")}-package: {key} ({type}, {(bytes == null ? 0 : bytes.Length)})".Log();
            if (onReceive != null) onReceive(key, type, bytes, tcp);
        }

        public virtual void Send<T>(byte key, T value, bool tcp)
        {
            if (tcp) Tcp.send(key, value);
            else Udp.send(key, value);
        }

        protected virtual void _connect(string tcpHost, int tcpPort, string udpHost, int udpPort)
        {
            Tcp = new TcpHandler(tcpHost, tcpPort, _receiveTcp, _disconnect);
            Udp = new UdpHandler(udpHost, udpPort, _receiveUdp);
        }

        public virtual void _disconnect()
        {
            if (Tcp != null) Tcp.Close();
            if (Udp != null) Udp.Close();

            "disconnected.".LogError();
        }
    }
}