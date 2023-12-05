using Cutulu;

namespace Walhalla
{
    public class Network
    {
        public TcpHandler Tcp;
        public UdpHandler Udp;

        public bool Connected => Tcp != null && Tcp.Connected;

        public Network(string tcpHost, int tcpPort, string udpHost, int udpPort)
        {
            try { _connect(tcpHost, tcpPort, udpHost, udpPort); }
            catch { $"Failed to connect to host".LogError(); }
        }

        private void receiveUdp(byte key, BufferType type, byte[] data) => Receive(key, type, data, false);
        private void receiveTcp(byte key, BufferType type, byte[] data) => Receive(key, type, data, true);
        public virtual void Receive(byte key, BufferType type, byte[] bytes, bool tcp)
        {
            $"{(tcp ? "tcp" : "udp")}-package: {key} ({type}, {(bytes == null ? 0 : bytes.Length)})".Log();
        }

        public virtual void Send<T>(byte key, T value, bool tcp)
        {
            if (tcp) Tcp.send(key, value);
            else Udp.send(key, value);
        }

        protected virtual void _connect(string tcpHost, int tcpPort, string udpHost, int udpPort)
        {
            Tcp = new TcpHandler(tcpHost, tcpPort, receiveTcp, Disconnect);
            Udp = new UdpHandler(udpHost, udpPort, receiveUdp);
        }

        public virtual void Disconnect()
        {
            if (Tcp != null) Tcp.Close();
            if (Udp != null) Udp.Close();

            "disconnected.".LogError();
        }
    }
}