using Cutulu;

namespace Walhalla
{
    public class Network
    {
        public TcpHandler Tcp;
        public UdpHandler Udp;

        public bool Connected => Tcp.Connected;

        public Network(string tcpHost, int tcpPort, string udpHost, int udpPort)
        {
            Tcp = new TcpHandler(tcpHost, tcpPort, receiveTcp, Disconnect);
            Udp = new UdpHandler(udpHost, udpPort, receiveUdp);
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

        public virtual void Disconnect()
        {
            "disconnected.".LogError();
        }
    }
}