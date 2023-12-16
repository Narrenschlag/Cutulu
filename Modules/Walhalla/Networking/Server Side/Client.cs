namespace Walhalla
{
    public class Client
    {
        public AdvancedClient Source;

        public bool isLoggedIn;
        public uint AccountId;

        public ClientTarget Target;
        public uint TargetId;
        public string Name;

        public Client(AdvancedClient advancedClient, ClientTarget target)
        {
            Source = advancedClient;

            advancedClient.onReceiveAll += onReceive;
            advancedClient.onClose += onQuit;

            setTarget(target);
        }

        public uint uid() => Source.UID;
        public uint tid() => TargetId;

        public void login(uint accountId, string name)
        {
            AccountId = accountId;
            isLoggedIn = true;

            Name = name;
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
            => Source.send(key, value, tcp);

        private void onQuit(ClientBase client)
        {
            if (Target != null)
                lock (Target) Target.remove(this);
        }
    }
}