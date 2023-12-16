using Godot;

namespace Walhalla.Client
{
    public partial class ClientTarget : Node
    {
        public virtual void remove() { }
        public virtual uint add() => 0;

        public virtual void receive(byte key, BufferType type, byte[] bytes) { }
        public virtual void _receive(byte key, BufferType type, byte[] bytes) { }
    }
}