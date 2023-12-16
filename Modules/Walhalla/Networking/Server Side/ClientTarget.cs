using Godot;

namespace Walhalla.Server
{
    public partial class ClientTarget : Node
    {
        /// <summary> On Client removal </summary>
        public virtual void remove(Client client) { }

        /// <summary> On Client addition </summary>
        public virtual uint add(Client client) => 0;

        /// <summary> On Client disconnect </summary>
        public virtual void disconnected(Client client) => remove(client);



        /// <summary> Receive TCP data </summary>
        public virtual void receive(Client client, byte key, BufferType type, byte[] bytes) { }

        /// <summary> Receive UDP data </summary>
        public virtual void _receive(Client client, byte key, BufferType type, byte[] bytes) { }
    }
}