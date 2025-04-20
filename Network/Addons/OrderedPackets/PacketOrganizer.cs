namespace Cutulu.Network.Addons
{
    using Cutulu.Core;

    /// <summary>
    /// PacketOrganizer is a utility class that helps you to organize packets
    /// <para>Espescially useful for ordered UDP packets</para>
    /// </summary>
    public partial class PacketOrganizer : TimeStamper<short>
    {
        /// <summary>
        /// Packs data into a time stamped byte array
        /// </summary>
        public byte[] Pack(short _key, object _obj)
        {
            return new OrderedPacket(Next(_key), _obj).Encode();
        }

        /// <summary>
        /// Unpacks data from a byte array and checks if a timestamp can be found aswell as if it is valid
        /// <para>Else tries to unpack the data as a normal packet</para>
        /// </summary>
        public bool TryUnpack<T>(short _key, byte[] _buffer, out T _decoded)
        {
            // Is an ordered packet
            if (_buffer.TryDecode(out OrderedPacket _packet))
            {
                _decoded = default;

                return IsValid(_key, _packet.Timestamp) && _packet.Buffer.TryDecode(out _decoded);
            }

            // Is not an ordered packet
            return _buffer.TryDecode(out _decoded);
        }

        /// <summary>
        /// Unpacks data from a byte array and checks if a timestamp can be found aswell as if it is valid
        /// <para>Else tries to unpack the data as a normal packet</para>
        /// <para>If the data is not valid it returns the default value</para>
        /// </summary>
        public T Unpack<T>(short _key, byte[] _buffer, T _default = default)
        {
            return TryUnpack(_key, _buffer, out T _decoded) ? _decoded : _default;
        }
    }
}