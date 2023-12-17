namespace Walhalla
{
    public class Target
    {
        public virtual void _receive(byte key, BufferType type, byte[] bytes, Method method, params object[] values) { }

        public virtual void _add(params object[] value) { }
        public virtual void _rem(params object[] value) { }

        public virtual void _disconnect(params object[] values) { }
    }
}