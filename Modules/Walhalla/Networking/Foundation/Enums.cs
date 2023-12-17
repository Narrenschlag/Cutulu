namespace Walhalla
{
    public enum Method
    {
        Tcp, Udp
    }

    public enum BufferType
    {
        None = 0,

        Boolean, Byte, ByteArray,
        Short, UnsignedShort,
        Integer, UnsignedInteger,
        Float, Double,
        String, Char,
        Json
    }
}