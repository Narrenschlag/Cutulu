using System.Net;

namespace Cutulu
{
    public class NetworkBrother
    {
        public readonly NetworkFamily Family;
        public readonly string RemoteHost;
        public readonly int RemotePort;

        private readonly TcpProtocol Remote;
        public readonly string RemoteId;

        /// <summary>
        /// Returns connection status
        /// </summary>
        public bool Connected => Remote != null && Remote.Connected;

        #region Constructor
        public NetworkBrother(PendingBrother brother, string remoteId, int remotePort)
        {
            Family = brother.Family;
            Remote = brother.Remote;

            RemoteHost = ((IPEndPoint)Remote.client.Client.RemoteEndPoint).Address.ToString();
            RemotePort = remotePort;
            RemoteId = remoteId;

            Remote.onDisconnect = OnDisconnect;
            Remote.onReceive = OnReceive;
        }

        public NetworkBrother(NetworkFamily family, ref string remoteHost, ref int remotePort, ref string remoteId, ref Passkey remoteKey)
        {
            RemoteId = remoteId;

            Family = family;
            Remote = new(
                RemoteHost = remoteHost,
                RemotePort = remotePort,
                OnReceive,
                OnDisconnect
            );
        }
        #endregion

        #region Delegate
        /// <summary>
        /// Triggered when data is received
        /// </summary>
        protected virtual void OnReceive(byte key, byte[] bytes, Method method)
        {

        }

        /// <summary>
        /// Triggered when connection is closed
        /// </summary>
        protected virtual void OnDisconnect()
        {
            Debug.LogError($"Brother is not welcome in this family.");

            Family?.OnDisconnect(this);
        }
        #endregion

        #region Public
        /// <summary>
        /// Sends data to brother
        /// </summary>
        public void Send<T>(byte key, T value)
        {
            if (Connected) Remote.Send(key, value);
        }

        /// <summary>
        /// Transfers connection to brother
        /// </summary>
        public void Transfer<Connection, Receiver>() where Connection : Marker<Receiver> where Receiver : Cutulu.Receiver
        {

        }

        /// <summary>
        /// Close connection on call
        /// </summary>
        public virtual void Close()
        {
            Remote?.Close();
        }
        #endregion
    }
}