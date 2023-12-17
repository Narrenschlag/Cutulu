namespace Walhalla
{
    public partial class Target : Godot.Node
    {
        public virtual void __receive(byte key, BufferType type, byte[] bytes, Method method, params object[] values) { }

        public virtual void __add(params object[] value) { }
        public virtual void __rem(params object[] value) { }

        public virtual void __disconnect(params object[] values) { }
    }
}