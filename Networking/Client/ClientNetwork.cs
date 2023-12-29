using System.Threading.Tasks;

namespace Cutulu
{
    public class ClientNetwork<T> : Marker<T> where T : Destination
    {
        public TcpProtocol Tcp;
        public UdpProtocol Udp;

        private bool udpAssociated;

        public bool Connected => Tcp != null && Tcp.Connected();

        public ClientNetwork(string tcpHost, int tcpPort, string udpHost, int udpPort, T destination = null) : base(0, destination)
        {
            try { _connect(tcpHost, tcpPort, udpHost, udpPort); }
            catch { $"Failed to connect to host".LogError(); }
        }

        public override void _receive(byte key, BufferType type, byte[] bytes, Method method)
        {
            // Notify client that server has successfully associated the udp client with the tcp client
            if (udpAssociated == false && key == 0 && type == BufferType.Byte)
            {
                if (bytes.As<byte>() == 255)
                {
                    udpAssociated = true;
                    return;
                }
            }

            base._receive(key, type, bytes, method);
        }

        public virtual void Send<V>(byte key, V value, Method method = Method.Tcp)
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

        /// <summary>
        /// Closes current connections and opens new connections
        /// </summary>
        protected virtual void _connect(string tcpHost, int tcpPort, string udpHost, int udpPort)
        {
            udpAssociated = false;

            if (Tcp != null)
            {
                Tcp.Close();
            }

            if (Udp != null)
            {
                Udp.Close();
            }

            Tcp = new TcpProtocol(tcpHost, tcpPort, _receive, _disconnect);
            Udp = new UdpProtocol(udpHost, udpPort, _receive);

            _setupUdp();
        }

        /// <summary> 
        /// Sends udp packages to server until<br/> 
        /// server associated tcp connection with udp connection 
        /// </summary>
        private async void _setupUdp()
        {
            // Stop if connections associated
            if (udpAssociated == true)
            {
                return;
            }

            // Send one byte udp packet with key 0
            Send(0, (byte)0, Method.Udp);

            // Wait 0.05s to resend association package
            await Task.Delay(50);

            // Restart the function
            _setupUdp();
        }

        /// <summary>
        /// Called on disconnection from server or network provider
        /// </summary>
        protected override void _disconnect()
        {
            if (Tcp != null) Tcp.Close();
            if (Udp != null) Udp.Close();

            base._disconnect();

            "disconnected.".LogError();
        }
    }
}