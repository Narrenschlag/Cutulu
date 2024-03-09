using System.Collections.Generic;

namespace Cutulu
{
    public class S2SConnection : ClientNetwork<S2SDestination>
    {
        public static Dictionary<string, S2SConnection> Established { get; private set; } = new();

        public Passkey Key { get; private set; }
        public string Host { get; private set; }

        public S2SConnection(string host, int tcpPort, int udpPort, S2SDestination destination) : base(host, tcpPort, host, udpPort, destination)
        {
            // Generate random 256 byte long passkey
            Key = new();

            // Assign host
            Host = host = host.Trim();

            if (Established.TryGetValue(host, out var existing) && existing != null)
            {
                Established[Host] = this;
                existing.Close();
            }

            else Established.Add(Host, this);
        }

        public override void Close()
        {
            base.Close();

            Established.TryRemove(Host);
        }
    }
}