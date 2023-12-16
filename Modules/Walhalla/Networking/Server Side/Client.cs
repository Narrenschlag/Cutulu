using Cutulu;

namespace Walhalla.Server
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

            advancedClient.onReceive += onReceive;
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
        protected virtual void onReceive(byte key, BufferType type, byte[] bytes, bool tcp)
        {
            if (Target.NotNull())
                try
                {
                    lock (Target)
                    {
                        if (tcp) Target.receive(this, key, type, bytes);
                        else Target._receive(this, key, type, bytes);
                    }
                }
                catch (System.Exception ex)
                {
                    $"[Client base]: cannot receive packet because {ex.Message}".LogError();
                }
        }

        public virtual void send<T>(byte key, T value, bool tcp)
            => Source.send(key, value, tcp);

        protected virtual void onQuit(ClientBase client)
        {
            if (Target != null)
                lock (Target) Target.remove(this);
        }
    }
}