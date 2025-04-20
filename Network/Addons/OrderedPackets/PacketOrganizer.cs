using Cutulu.Core;

namespace Cutulu.Network.Addons
{
    public partial class PacketOrganizer : Core.TimeStamper<short>
    {
        public PacketOrganizer() : base()
        {

        }

        public byte[] Pack(short _key, object _obj)
        {
            return new OrderedPacket(Next(_key), _obj).Encode();
        }

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

        public T Unpack<T>(short _key, byte[] _buffer, T _default = default)
        {
            return TryUnpack(_key, _buffer, out T _decoded) ? _decoded : _default;
        }
    }
}