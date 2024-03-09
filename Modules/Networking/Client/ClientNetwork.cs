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

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ClientNetwork(string tcpHost, int tcpPort, string udpHost, int udpPort, T destination = null) : base(0, 0, destination)
        {
            try { Connect(tcpHost, tcpPort, udpHost, udpPort); }
            catch { $"Failed to connect to host".LogError(); }
        }

        /// <summary>
        /// Closes current connections and opens new connections
        /// </summary>
        protected virtual void Connect(string tcpHost, int tcpPort, string udpHost, int udpPort)
        {
            Close();

            Tcp = new TcpProtocol(tcpHost, tcpPort, Receive, Disconnected);
            Udp = new UdpProtocol(udpHost, udpPort, Receive);

            if (TcpConnected = Tcp != null && Tcp.Connected)
            {
                SetupUdp();
            }
        }

        /// <summary> 
        /// Sends udp packages to server until<br/> 
        /// server associated tcp connection with udp connection 
        /// </summary>
        private async void SetupUdp()
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
            SetupUdp();
        }
        #endregion

        #region Send Data       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Sends data to server
        /// </summary>
        public virtual void Send<V>(byte key, V value, Method method = Method.Tcp)
        {
            switch (method)
            {
                case Method.Tcp:
                    Tcp.Send(key, value);
                    break;

                case Method.Udp:
                    Udp.Send(key, value, SafetyId);
                    break;

                default: break;
            }
        }
        #endregion

        #region Receive Data    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Receives data from server
        /// </summary>
        public override void Receive(byte key, byte[] bytes, Method method)
        {
            // Notify client that server has successfully associated the udp client with the tcp client
            if (UdpConnected == false && key == 255)
            {
                if (bytes.TryBuffer(out ushort safetyId))
                {
                    SafetyId = safetyId;
                    UdpConnected = true;
                    return;
                }
            }

            base.Receive(key, bytes, method);
        }
        #endregion

        #region End Connection  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called on disconnection from server or network provider
        /// </summary>
        protected override void Disconnected()
        {
            Close();

            base.Disconnected();

            "disconnected.".LogError();
        }

        /// <summary> 
        /// Closes connection to server if connected
        /// </summary>
        public virtual void Close()
        {
            UdpConnected = false;
            TcpConnected = false;

            Tcp?.Close();
            Udp?.Close();
        }
        #endregion
    }
}