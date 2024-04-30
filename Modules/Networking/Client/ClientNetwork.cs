using System.Net;
using System.Threading.Tasks;

namespace Cutulu
{
    public class ClientNetwork<R> : Peer<R> where R : Receiver
    {
        public Protocol.Empty OnSetupCompleteEvent;

        public TcpProtocol Tcp;
        public UdpProtocol Udp;

        public bool UdpConnected;
        public bool TcpConnected;

        public IPType IPType { get; private set; }

        public bool FullyConnected() => TcpConnected && UdpConnected;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ClientNetwork(string tcpHost, int tcpPort, string udpHost, int udpPort, R receiver = null, Protocol.Empty onSetupComplete = null, IPType listenTo = IPType.Any) : base(0, 0, receiver)
        {
            OnSetupCompleteEvent = onSetupComplete;

            try { Connect(tcpHost, tcpPort, udpHost, udpPort, listenTo); }
            catch { $"Failed to connect to host".LogError(); }
        }

        /// <summary>
        /// Closes current connections and opens new connections
        /// </summary>
        protected virtual void Connect(string tcpHost, int tcpPort, string udpHost, int udpPort, IPType listenTo)
        {
            Close();

            IPType = listenTo;

            Tcp = new TcpProtocol(tcpHost, tcpPort, Receive, Disconnected, IPType);
            Udp = new UdpProtocol(udpHost, udpPort, Receive, IPType);
        }

        /// <summary> 
        /// Triggered when setup is complete
        /// </summary>
        protected virtual void OnSetupComplete()
        {
            OnSetupCompleteEvent?.Invoke();
        }
        #endregion

        #region Send Data       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Sends data to server
        /// </summary>
        public virtual void Send<V>(short key, V value, Method method)
        {
            switch (method)
            {
                case Method.Tcp:
                    Tcp?.Send(ref key, value);
                    break;

                case Method.Udp:
                    Udp?.Send(ref key, value, SafetyId);
                    break;

                default: break;
            }
        }
        /// <summary>
        /// Sends signal to server
        /// </summary>
        public virtual void Send(short key, Method method)
        {
            switch (method)
            {
                case Method.Tcp:
                    Tcp.Send<byte[]>(ref key, null);
                    break;

                case Method.Udp:
                    Udp.Send<byte[]>(ref key, null, SafetyId);
                    break;

                default: break;
            }
        }
        #endregion

        #region Receive Data    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool welcomeHasBeenReceived;

        /// <summary>
        /// Receives data from server
        /// </summary>
        public override void Receive(ref NetworkPackage package)
        {
            switch (package.Key)
            {
                // Triggers udp port sending as soon as the tcp is ready
                case TcpProtocol.WelcomeKey:
                    if (welcomeHasBeenReceived == false)
                    {
                        welcomeHasBeenReceived = true;

                        //Debug.Log($"Client to Server: {((IPEndPoint)Udp.client.Client.LocalEndPoint).Port}");
                        Send(11111, ((IPEndPoint)Udp.client.Client.LocalEndPoint).Port, Method.Tcp);
                    }
                    break;

                // Notify client that server has successfully associated the udp client with the tcp client
                case 255:
                    if (UdpConnected == false && package.TryBuffer(out ushort safetyId))
                    {
                        SafetyId = safetyId;
                        UdpConnected = true;

                        OnSetupComplete();
                        return;
                    }
                    break;

                default: break;
            }

            base.Receive(ref package);
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
            Tcp = null;

            Udp?.Close();
            Udp = null;
        }
        #endregion
    }
}