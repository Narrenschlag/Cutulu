namespace Cutulu
{
    public class S2SConnection<R> : ClientNetwork<R> where R : S2SSetup
    {
        public Passkey RemoteKey { get; private set; }
        public string Host { get; private set; }

        public S2SConnection(string host, int tcpPort, int udpPort, R receiver) : base(host, tcpPort, host, udpPort, receiver)
        {
            
        }

        protected override void OnSetupComplete()
        {
            
        }

        public override void Close()
        {
            
        }
    }
}