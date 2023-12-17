namespace Walhalla
{
    public class Delegates
    {
        public delegate void Packet(byte key, BufferType type, byte[] bytes, Method method);
        public delegate void Empty();
    }
}