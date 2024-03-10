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

        public readonly TcpListener Listener;
        private readonly Passkey LocalKey;
        private readonly string LocalId;

        /// <summary>
        /// Compare given passkey to local passkey and return if it's valid
        /// </summary>
        public bool CompareKey(Passkey key) => LocalKey.Compare(ref key);

        /// <summary>
        /// Careful. Remote Keys are only assigned once and here.
        /// </summary>
        public NetworkFamily(string localId, int localPort, Passkey localKey, params RemoteKey[] remoteKeys)
        {
            // Establish remote keys
            RemoteKeys = new(remoteKeys ?? Array.Empty<RemoteKey>());

            // Establish local key and brother registry
            EstablishedBrothers = new();
            LocalKey = localKey;
            LocalId = localId;

            // Establish listener
            Listener = new(IPAddress.Any, localPort);
            Listener.Start(16);

            // Async client accept
            Accept();
        }

        #region Receive Connection
        /// <summary>
        /// Handles all incomming connections and assigns them to an id.
        /// Then it starts listening to them.
        /// </summary>
        protected virtual async void Accept()
        {
            // If a connection exists, the server will accept it
            var client = await Listener.AcceptTcpClientAsync();

            Debug.LogError($"Pending brother request from {client.GetAddressPort()}");
            var pending = new PendingBrother(client, this, RemoteKeys, OnWelcomeBrother);

            // Welcome other clients
            Accept();
        }

        private void OnWelcomeBrother(NetworkBrother brother)
        {

        }
        #endregion

        #region Create Connection
        /// <summary>
        /// Establish connection to remote brother
        /// </summary>
        public void Connect(string remoteHost, int remotePort)
        {
            if (RemoteKeys.TryGetValue($"{host.Trim()}", out var remoteKey))
            {
                
            }

            else Debug.LogError($"No remote keys for {host.Trim()} established");
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

        #region Connection Utility
        /// <summary>
        /// Add brother to registry
        /// </summary>
        private NetworkBrother AddBrother(NetworkBrother brother)
        {
            if (EstablishedBrothers.TryGetValue(brother.RemoteId, out var oldBrother))
            {
                EstablishedBrothers[brother.RemoteId] = brother;
                oldBrother?.Close();
            }

            else EstablishedBrothers.Add(brother.RemoteId, brother);

            return brother;
        }

        /// <summary>
        /// Triggered when brother disconnects
        /// </summary>
        public void OnDisconnect(NetworkBrother brother)
        {
            EstablishedBrothers.TryRemove(brother.RemoteId);
        }
        #endregion
    }
}