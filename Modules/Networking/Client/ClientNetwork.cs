using System.Threading.Tasks;

namespace Cutulu
{
    public class ClientNetwork<T> : Marker<T> where T : Destination
    {
        public TcpProtocol Tcp;
        public UdpProtocol Udp;

        public bool UdpConnected;
        public bool TcpConnected;

        public bool FullyConnected() => TcpConnected && UdpConnected;

        public ClientNetwork(string tcpHost, int tcpPort, string udpHost, int udpPort, T destination = null) : base(0, destination)
        {
            try { _connect(tcpHost, tcpPort, udpHost, udpPort); }
            catch { $"Failed to connect to host".LogError(); }
        }

        public override void _receive(byte key, byte[] bytes, Method method)
        {
            // Notify client that server has successfully associated the udp client with the tcp client
            if (UdpConnected == false && key == 0)
            {
                if (bytes.TryDeserialize(out byte auth) && auth == 255)
                {
                    UdpConnected = true;
                    return;
                }
            }

            base._receive(key, bytes, method);
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
            UdpConnected = false;
            close();

            Tcp = new TcpProtocol(tcpHost, tcpPort, _receive, _disconnect);
            Udp = new UdpProtocol(udpHost, udpPort, _receive);

            if (TcpConnected = Tcp != null && Tcp.Connected)
            {
                _setupUdp();
            }
        }

        /// <summary> 
        /// Sends udp packages to server until<br/> 
        /// server associated tcp connection with udp connection 
        /// </summary>
        private async void _setupUdp()
        {
            // Stop if connections associated
            if (UdpConnected == true)
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
            close();

            base._disconnect();

            "disconnected.".LogError();
        }

        protected virtual void close()
        {
            UdpConnected = false;
            TcpConnected = false;

            Tcp?.Close();
            Udp?.Close();
        }
    }
}