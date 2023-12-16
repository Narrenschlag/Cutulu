using Godot;

namespace Walhalla.Client
{
    public partial class ClientTarget : Node
    {
        /// <summary> On Client removal </summary>
        public virtual void remove() { }

        /// <summary> On Client addition </summary>
        public virtual void add() { }

        /// <summary> On Client disconnect </summary>
        public virtual void disconnected() => remove();



        /// <summary> Receive TCP data </summary>
        public virtual void receive(byte key, BufferType type, byte[] bytes) { }

        /// <summary> Receive UDP data </summary>
        public virtual void _receive(byte key, BufferType type, byte[] bytes) { }
    }
}