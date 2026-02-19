namespace Cutulu.Core;

using System.Collections.Generic;

/// <summary>
/// Written by Maximilian Schecklmann on 19th of Feb 2026. Does what it i's named after.
/// </summary>
public class TwoWayDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    private readonly Dictionary<TValue, TKey> Reverse = [];

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        Reverse.Add(value, key);
    }

    public void Add(TValue value, TKey key) => Add(key, value);

    public new bool Remove(TKey key)
    {
        if (base.Remove(key))
        {
            Reverse.Remove(this[key]);
            return true;
        }

        return false;
    }

    public bool Remove(TValue value)
    {
        if (Reverse.TryGetValue(value, out var key))
        {
            if (base.Remove(key))
            {
                Reverse.Remove(value);
                return true;
            }
        }

        return false;
    }

    public new TValue this[TKey key]
    {
        get => base[key];
        set
        {
            base[key] = value;
            Reverse[value] = key;
        }
    }

    public TKey this[TValue val]
    {
        get => Reverse[val];
        set
        {
            Reverse[val] = value;
            this[value] = val;
        }
    }

    public bool TryGetKey(TValue val, out TKey key)
    {
        return Reverse.TryGetValue(val, out key);
    }
}