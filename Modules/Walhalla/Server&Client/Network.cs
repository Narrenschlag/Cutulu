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
            Tcp = new TcpHandler(udpHost, tcpPort, receiveTcp, Disconnect);
            Udp = new UdpHandler(udpHost, udpPort, receiveUdp);
        }

        private void receiveUdp(byte key, BufferType type, byte[] data) => Receive(type, key, data, false);
        private void receiveTcp(byte key, BufferType type, byte[] data) => Receive(type, key, data, true);
        public virtual void Receive(BufferType type, byte key, byte[] data, bool tcp)
        {
            $"[{(tcp ? "TCP" : "UDP")}] ".Log();
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