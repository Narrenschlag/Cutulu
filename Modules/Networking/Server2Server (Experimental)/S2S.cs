using System.Collections.Generic;

namespace Cutulu
{
    using Client = ClientNetwork<S2SSetup>;
    using Connection = ServerConnection<S2SSetup>;
    public static class S2S
    {
        public static List<Connection> EstablishedBrothers { get; private set; } = new();

        private static Passkey _hiddenPasskey;
        private static Passkey HiddenPasskey
        {
            set => _hiddenPasskey = value;

            get
            {
                if (_hiddenPasskey.Key == null)
                {
                    _hiddenPasskey = new();
                }

                return _hiddenPasskey;
            }
        }

        public static bool TryBond(Connection connection, Passkey key)
        {
            // Validate pass key for bonding 
            if (HiddenPasskey.Validate(ref key))
            {
                EstablishedBrothers.Add(connection);
                return true;
            }

            return false;
        }

        public static void EstablishConnection(Connection connection, Passkey key)
        {
            // Try bonding
            var success = TryBond(connection, key);
        }

        public static void EstablishConnection(string host, int tcpPort, int udpPort, Receiver receiver)
        {
            var _receiver = new S2SSetup(OnBond);

            var sender = new Client(host, tcpPort, host, udpPort, _receiver);
        }

        private static void OnBond(Client client, Passkey key)
        {
            // Convert client to connection
            var connection = new Connection(client);

            // Try bonding
            var success = TryBond(connection, key);
        }
    }
}