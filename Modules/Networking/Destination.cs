namespace Cutulu
{
    public partial class Destination : Godot.Node
    {
        public virtual void __receive(byte key, byte[] bytes, Method method, params object[] values) { }

        public virtual void __add(params object[] value) { }
        public virtual void __rem(params object[] value) { }

        public virtual void __disconnect(params object[] values) => __rem(values);
    }
}