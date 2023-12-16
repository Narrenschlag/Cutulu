namespace Walhalla.Client
{
    public class Client
    {
        public Network Source;

        public bool isLoggedIn;
        public uint AccountId;

        public ClientTarget Target;
        public uint TargetId;
        public string Name;

        public Client(Network network, ClientTarget target)
        {
            Source = network;

            Network.onReceive += onReceive;
            Network.onDisconnect += onQuit;

            setTarget(target);
        }

        public void setTarget(ClientTarget target)
        {
            if (Target != null) Target.remove(this);
            Target = target;

            TargetId = target != null ? target.add(this) : 0;
        }

        // receive		tcp results
        // _receive		udp results
        private void onReceive(byte key, BufferType type, byte[] bytes, bool tcp)
        {
            if (Target != null)
                lock (Target)
                {
                    if (tcp) Target.receive(this, key, type, bytes);
                    else Target._receive(this, key, type, bytes);
                }
        }

        public void send<T>(byte key, T value, bool tcp)
            => Source.Send(key, value, tcp);

        private void onQuit()
        {
            if (Target != null)
                lock (Target) Target.remove(this);
        }
    }
}