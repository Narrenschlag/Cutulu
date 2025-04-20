namespace Cutulu.Network
{
    using System.Collections.Generic;
    using System.Numerics;

    /// <summary>
    /// Timestamper is a utility class that helps you to timestamp packets
    /// </summary>
    public partial class TimeStamper<STAMP> where STAMP : struct, INumber<STAMP>, IMinMaxValue<STAMP>, IDivisionOperators<STAMP, STAMP, STAMP>
    {
        private readonly static STAMP HalfDiv = STAMP.CreateChecked(2);
        private readonly Dictionary<object, STAMP> Timestamps = [];

        /// <summary>
        /// Checks if a timestamp is valid and updates the timestamp if it is
        /// </summary>
        public bool IsValid(object _key, OrderedPacket<STAMP> _packet)
        {
            return IsValid(_key, _packet.Timestamp);
        }

        /// <summary>
        /// Checks if a timestamp is valid and updates the timestamp if it is
        /// </summary>
        public bool IsValid(object _key, STAMP _stamp)
        {
            if (Timestamps.TryGetValue(_key, out var _timestamp))
            {
                // Check if timestamp is too old
                if (_stamp < _timestamp && _timestamp - _stamp < STAMP.MaxValue / HalfDiv) return false;
            }

            Timestamps[_key] = _stamp;
            return true;
        }

        /// <summary>
        /// Gets next timestamp and updates to the returned value
        /// </summary>
        public STAMP Next(object _key)
        {
            var _stamp = Timestamps.TryGetValue(_key, out var _timestamp) ? _timestamp : STAMP.Zero;
            Timestamps[_key] = _stamp += STAMP.One;
            return _stamp;
        }

        /// <summary>
        /// Gets current timestamp
        /// </summary>
        public STAMP Current(object _key)
        {
            return Timestamps.TryGetValue(_key, out var _timestamp) ? _timestamp : STAMP.Zero;
        }

        /// <summary>
        /// Clears all timestamps
        /// </summary>
        public void Clear()
        {
            Timestamps.Clear();
        }
    }
}