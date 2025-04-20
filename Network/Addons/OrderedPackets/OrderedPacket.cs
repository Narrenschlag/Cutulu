namespace Cutulu.Network.Addons
{
    using Core;

    /// <summary>
    /// OrderedPacket is a utility class that helps you to timestamp packets
    /// <para>Espescially useful for ordered UDP packets</para>
    /// </summary>
    public class OrderedPacket
    {
        public short Timestamp { get; set; }
        public byte[] Buffer { get; set; }

        public OrderedPacket(short _timestamp, object _obj)
        {
            Buffer = _obj == null ? [] : _obj.Encode();
            Timestamp = _timestamp;
        }

        public OrderedPacket() { }
    }
}