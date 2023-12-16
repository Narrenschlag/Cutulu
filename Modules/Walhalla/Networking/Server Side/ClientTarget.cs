using Godot;

namespace Walhalla
{
    public partial class ClientTarget : Node
    {
        public virtual void remove(Client client) { }
        public virtual uint add(Client client) => 0;

        public virtual void receive(Client client, byte key, BufferType type, byte[] bytes) { }
        public virtual void _receive(Client client, byte key, BufferType type, byte[] bytes) { }
    }
}