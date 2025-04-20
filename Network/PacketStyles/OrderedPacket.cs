namespace Cutulu.Network
{
    using System.Numerics;
    using Core;

    /// <summary>
    /// OrderedPacket is a utility class that helps you to timestamp packets
    /// <para>Espescially useful for ordered UDP packets</para>
    /// </summary>
    public class OrderedPacket<STAMP> where STAMP : struct, INumber<STAMP>, IMinMaxValue<STAMP>, IDivisionOperators<STAMP, STAMP, STAMP>
    {
        public STAMP Timestamp { get; set; }
        public byte[] Buffer { get; set; }

        public OrderedPacket(STAMP _timestamp, object _obj)
        {
            Buffer = _obj == null ? [] : _obj.Encode();
            Timestamp = _timestamp;
        }

        public OrderedPacket(TimeStamper<STAMP> _timestamper, object _stamp_key, object _obj)
        {
            Buffer = _obj == null ? [] : _obj.Encode();
            Timestamp = _timestamper.Next(_stamp_key);
        }

        public OrderedPacket() { }

        #region Local Functions

        public bool TryUnpack<T>(out T _value)
        {
            return Buffer.TryDecode(out _value);
        }

        public T Unpack<T>(T _default = default)
        {
            return TryUnpack(out T t) ? t : _default;
        }

        public byte[] Pack()
        {
            return this.Encode();
        }

        #endregion

        #region Static Functions

        public static bool TryUnpack<T>(byte[] _buffer, out T _value)
        {
            if (_buffer.TryDecode(out OrderedPacket<STAMP> _packet))
            {
                return _packet.TryUnpack(out _value);
            }

            _value = default;
            return false;
        }

        public static T Unpack<T>(byte[] _buffer, T _default = default)
        {
            return TryUnpack(_buffer, out T t) ? t : _default;
        }

        public static byte[] Pack(STAMP _timestamp, object _obj)
        {
            return new OrderedPacket<STAMP>(_timestamp, _obj).Pack();
        }

        public static byte[] Pack(TimeStamper<STAMP> _timestamper, object _stamp_key, object _obj)
        {
            return new OrderedPacket<STAMP>(_timestamper.Next(_stamp_key), _obj).Pack();
        }

        #endregion
    }
}