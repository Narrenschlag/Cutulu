using System.Collections.Generic;

namespace Cutulu
{
    public static class S2S<D> where D : S2SReceiver
    {
        public static List<ServerConnection<D>> EstablishedBrothers { get; private set; } = new();

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

        public static bool TryBond(ServerConnection<D> connection, Passkey key)
        {
            // Validate pass key for bonding 
            if (HiddenPasskey.Validate(ref key))
            {
                EstablishedBrothers.Add(connection);
                return true;
            }

            return false;
        }

        public static ServerConnection<D> EstablishConnection(string host, int tcpPort, int udpPort, D receiver)
        {
            return default;
        }
    }
}