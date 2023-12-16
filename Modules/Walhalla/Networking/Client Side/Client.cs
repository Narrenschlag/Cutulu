using Cutulu;

namespace Walhalla.Client
{
    public class Client
    {
        public Network Source;

        public ClientTarget Target;

        public Client(Network network, ClientTarget target)
        {
            Source = network;

            Network.onReceive += onReceive;
            Network.onDisconnect += onDisconnect;

            setTarget(target);
        }

        /// <summary> Sets target to receive data </summary>
        public void setTarget(ClientTarget target)
        {
            if (Target != null) Target.remove();

            Target = target;
            if (target.NotNull()) target.add();
        }

        // receive		tcp results
        // _receive		udp results
        protected virtual void onReceive(byte key, BufferType type, byte[] bytes, bool tcp)
        {
            if (Target != null)
                lock (Target)
                {
                    try
                    {
                        if (tcp) Target.receive(key, type, bytes);
                        else Target._receive(key, type, bytes);
                    }
                    catch (System.Exception ex)
                    {
                        $"[Client base]: cannot receive packet because {ex.Message}".LogError();
                    }
                }
        }

        /// <summary> Send data to server </summary>
        public virtual void send<T>(byte key, T value, bool tcp)
            => Source.Send(key, value, tcp);

        /// <summary> Triggered on server/client disconnect </summary>
        protected virtual void onDisconnect()
        {
            if (Target != null)
                lock (Target) Target.disconnected();
        }
    }
}