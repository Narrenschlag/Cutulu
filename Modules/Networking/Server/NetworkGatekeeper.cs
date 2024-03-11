using System.Collections.Generic;
using System;

namespace Cutulu
{
    /// <summary>
    /// Allows you to verify your incomming connections before actually allowing them to the server.
    /// </summary>
    public class NetworkGatekeeper<R> where R : Receiver
    {
        private readonly Action<ServerConnection<R>> Callback;
        private readonly Dictionary<string, byte> Expected;
        private readonly string[] Blacklist;
        private readonly Passkey LocalKey;

        /// <summary>
        /// Construct Gatekeeper and give orders
        /// </summary>
        public NetworkGatekeeper(Passkey localKey, Action<ServerConnection<R>> callback, Dictionary<string, byte> expected, params string[] blacklist)
        {
            Blacklist = blacklist;
            Expected = expected;

            Callback = callback;
            LocalKey = localKey;
        }

        /// <summary>
        /// Compare given passkey to local passkey and return if it's valid. 
        /// </summary>
        public bool CompareKey(byte[] key) => LocalKey.Compare(key);

        /// <summary>
        /// Check if connection is even allowed to enter the server. 
        /// </summary>
        public bool IsWelcome(ServerConnection<R> connection)
        {
            var address = connection.tcp.client.GetAddress().ToString();

            return
                (Expected == null || Expected.ContainsKey(address)) &&
                (Blacklist == null || Blacklist.Contains(address) == false);
        }

        /// <summary>
        /// Check if connection may just pass without stop. 
        /// </summary>
        public void Queue(ServerConnection<R> connection)
        {
            if (IsWelcome(connection) == false)
            {
                // Not welcome here
                connection.Close();
                return;
            }

            // No key is established therefore pass
            if (LocalKey.Key.IsEmpty()) Callback?.Invoke(connection);

            // Check for papers
            connection.onReceive2 += OnReceive;
        }

        /// <summary>
        /// Check first incomming packet for the right pass key. 
        /// </summary>
        private void OnReceive(ServerConnection<R> connection, byte gateKey, byte[] bytes, Method method)
        {
            // Check gate key
            switch (gateKey)
            {
                // Right gate key
                case 123:
                    // Check pass key
                    if (CompareKey(bytes))
                    {
                        // Pass Gatekeeper
                        connection.onReceive2 -= OnReceive;
                        Callback?.Invoke(connection);
                    }

                    // Incorrect pass key
                    break;

                // Wrong gate key
                default: break;
            }

            // Not allowed to pass
            connection.Close();
        }
    }
}