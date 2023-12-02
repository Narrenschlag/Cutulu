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

        private void receiveUdp(BufferType type, byte key, byte[] data) => Receive(type, key, data, false);
        private void receiveTcp(BufferType type, byte key, byte[] data) => Receive(type, key, data, true);
        public virtual void Receive(BufferType type, byte key, byte[] data, bool tcp)
        {

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