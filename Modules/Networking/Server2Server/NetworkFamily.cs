using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;

namespace Cutulu
{
    using RemoteKey = KeyValuePair<string, Passkey>;
    public class NetworkFamily
    {
        public readonly Dictionary<string, NetworkBrother> EstablishedBrothers;
        private readonly Dictionary<string, Passkey> RemoteKeys;

        private readonly TcpListener Listener;
        private readonly Passkey LocalKey;

        /// <summary>
        /// Compare given passkey to local passkey and return if it's valid
        /// </summary>
        public bool CompareKey(ref Passkey key) => LocalKey.Compare(ref key);

        /// <summary>
        /// Careful. Remote Keys are only assigned once and here.
        /// </summary>
        public NetworkFamily(int port, Passkey localKey, RemoteKey remoteKey, params RemoteKey[] remoteKeys)
        {
            // Establish remote keys
            RemoteKeys = new(remoteKeys ?? Array.Empty<RemoteKey>()) { { remoteKey.Key, remoteKey.Value } };

            // Establish local key and brother registry
            EstablishedBrothers = new();
            LocalKey = localKey;

            // Establish listener
            Listener = new(IPAddress.Any, port);
            Listener.Start(16);

            // Async client accept
            Accept();
        }

        #region Register Connection
        /// <summary>
        /// Add brother to registry
        /// </summary>
        private NetworkBrother AddBrother(NetworkBrother brother)
        {
            string host = brother.Host.ToString();

            if (EstablishedBrothers.TryGetValue(host, out var oldBrother))
            {
                EstablishedBrothers[host] = brother;
                oldBrother?.Close();
            }

            else EstablishedBrothers.Add(host, brother);

            return brother;
        }
        #endregion

        #region Receive Connection
        /// <summary>
        /// Handles all incomming connections and assigns them to an id.
        /// Then it starts listening to them.
        /// </summary>
        protected virtual async void Accept()
        {
            // If a connection exists, the server will accept it
            var client = await Listener.AcceptTcpClientAsync();

            if (RemoteKeys.TryGetValue(client.GetAddressPort(), out var remoteKey))
            {
                AddBrother(new(client, this, remoteKey));
            }

            else Debug.LogError($"Unexpected brother request from {client.GetAddressPort()}");

            // Welcome other clients
            Accept();
        }
        #endregion

        #region Create Connection
        /// <summary>
        /// Establish connection to remote brother
        /// </summary>
        public void EstablishConnection(string host, int port)
        {
            if (RemoteKeys.TryGetValue($"{host.Trim()}:{port}", out var remoteKey))
                _ = new NetworkBrother(host, port, remoteKey, this, OnConnectionSuccess, OnConnectionFailed);

            else Debug.LogError($"No remote keys for {host.Trim()}:{port} established");
        }

        /// <summary>
        /// Connection established successfully
        /// </summary>
        private void OnConnectionSuccess(NetworkBrother brother)
        {
            AddBrother(brother);
        }

        /// <summary>
        /// Connection failed
        /// </summary>
        private void OnConnectionFailed(NetworkBrother brother)
        {

        }
        #endregion

        #region End Connection
        /// <summary>
        /// Triggered when brother disconnects
        /// </summary>
        public void OnDisconnect(NetworkBrother brother)
        {
            EstablishedBrothers.TryRemove(brother.Host);
        }
        #endregion
    }
}