namespace Cutulu.Core;

using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Timestamper is a utility class that helps you to timestamp packets
/// </summary>
public partial class TimeStamper<STAMP>
    where STAMP :
        struct, INumber<STAMP>, IMinMaxValue<STAMP>,
        IDivisionOperators<STAMP, STAMP, STAMP>, IIncrementOperators<STAMP>
{
    private readonly static STAMP HalfMax = STAMP.MaxValue / STAMP.CreateChecked(2);
    private readonly Dictionary<object, STAMP> Timestamps = [];

    /// <summary>
    /// Checks if a timestamp is valid and updates the timestamp if it is.
    /// Handles wrap-around: a stamp is valid if it is "ahead" of the stored
    /// one within half the type's range.
    /// </summary>
    public bool IsValid(object _key, STAMP _stamp)
    {
        if (Timestamps.TryGetValue(_key, out var _current))
        {
            // Distance from current to incoming stamp, wrapping around
            STAMP delta = _stamp - _current;

            // delta == 0: duplicate; delta > HalfMax: stamp is behind (stale)
            if (delta == STAMP.Zero || delta > HalfMax) return false;
        }

        Timestamps[_key] = _stamp;
        return true;
    }

    /// <summary>
    /// Gets next timestamp and updates to the returned value.
    /// Wraps back to Zero after MaxValue.
    /// </summary>
    public STAMP Next(object _key)
    {
        var _current = Timestamps.TryGetValue(_key, out var _timestamp) ? _timestamp : STAMP.Zero;

        // Wrap around to Zero instead of overflowing
        var _next = _current == STAMP.MaxValue ? STAMP.Zero : _current + STAMP.One;

        Timestamps[_key] = _next;
        return _next;
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

    /// <summary>
    /// Clears given timestamp keys
    /// </summary>
    public void Clear(params object[] _keys)
    {
        if (_keys.IsEmpty()) return;

        foreach (var _key in _keys)
        {
            if (_key != null && Timestamps.ContainsKey(_key))
                Timestamps.Remove(_key);
        }
    }
}