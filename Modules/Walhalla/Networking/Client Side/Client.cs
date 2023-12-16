using System;
using Cutulu;

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
            if (Target != null) Target.remove();
            Target = target;

            TargetId = target != null ? target.add() : 0;
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
                    catch (Exception ex)
                    {
                        $"[Client base]: cannot receive packet because {ex.Message}".LogError();
                    }
                }
        }

        public virtual void send<T>(byte key, T value, bool tcp)
            => Source.Send(key, value, tcp);

        protected virtual void onQuit()
        {
            if (Target != null)
                lock (Target) Target.remove();
        }
    }
}