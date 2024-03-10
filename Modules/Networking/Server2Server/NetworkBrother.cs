using System.Threading.Tasks;
using System.Net.Sockets;
using System;

namespace Cutulu
{
    using Callback = Action<NetworkBrother>;
    public class NetworkBrother
    {
        public const int VerificationTimeWindow = 4;

        public readonly NetworkFamily Family;
        public readonly string Host;
        public readonly int Port;

        public bool Verified { get; private set; }
        private readonly Passkey RemoteKey;
        private readonly TcpProtocol Tcp;

        /// <summary>
        /// Returns connection status
        /// </summary>
        public bool Connected => Tcp != null && Tcp.Connected;

        #region Constructor
        /// <summary>
        /// Create connection
        /// </summary>
        public NetworkBrother(string host, int port, Passkey remoteKey, NetworkFamily family, Callback onSuccess, Callback onFail) : this(family, ref remoteKey)
        {
            Host = host.Trim();
            Port = port;

            Tcp = new(Host, Port, OnReceive, OnDisconnect);

            // Send key for approval
            Send(127, RemoteKey);

            // Notify family
            if (Connected) onSuccess?.Invoke(this);
            else onFail?.Invoke(this);
        }

        /// <summary>
        /// Receive connection
        /// </summary>
        public NetworkBrother(TcpClient client, NetworkFamily family, Passkey remoteKey) : this(family, ref remoteKey)
        {
            client.GetAddressPort(out Host, out Port);

            Tcp = new(ref client, 0, OnReceive, OnDisconnect, 0);
        }

        /// <summary>
        /// Local base constructor
        /// </summary>
        private NetworkBrother(NetworkFamily family, ref Passkey key)
        {
            Verified = false;
            RemoteKey = key;
            Family = family;

            StartTimeWindow();
        }
        #endregion

        #region Time Window
        /// <summary>
        /// Starts time window in which the verification process has to be completed. Else close connection.
        /// </summary>
        private async void StartTimeWindow()
        {
            await Task.Delay(1000 * VerificationTimeWindow);

            if (Verified == false) Close();
        }
        #endregion

        #region Delegate
        /// <summary>
        /// Triggered when data is received
        /// </summary>
        private void OnReceive(byte key, byte[] bytes, Method method)
        {
            // Verify Data
            if (Verified == false)
            {
                if (Connected == false) return;

                switch (key)
                {
                    // Verification request
                    case 127:
                        var localKey = new Passkey(bytes);
                        if (Family.CompareKey(ref localKey))
                        {
                            Send(128, RemoteKey);
                            Verified = true;
                        }
                        else Close();
                        break;

                    // Verification awnser
                    case 128:
                        localKey = new Passkey(bytes);
                        if (Family.CompareKey(ref localKey)) Verified = true;
                        else Close();
                        break;

                    // Invalid result
                    default:
                        Close();
                        break;
                }
            }

            // Handle data
            else
            {

            }
        }

        /// <summary>
        /// Triggered when connection is closed
        /// </summary>
        public virtual void OnDisconnect()
        {
            Family?.OnDisconnect(this);
        }
        #endregion

        #region Public
        /// <summary>
        /// Sends data to brother
        /// </summary>
        public void Send<T>(byte key, T value)
        {
            if (Connected) Tcp.Send(key, value);
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
            Tcp?.Close();
        }
        #endregion
    }
}