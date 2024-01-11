namespace Cutulu
{
    public partial class Destination : Godot.Node
    {
        public virtual void Receive(byte key, byte[] bytes, Method method, params object[] values) { }

        public virtual void Add(params object[] value) { }
        public virtual void Rem(params object[] value) { }

        public virtual void Disconnect(params object[] values) => Rem(values);
    }
}