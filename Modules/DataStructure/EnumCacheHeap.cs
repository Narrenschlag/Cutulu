using System.Collections.Generic;
using System;
using Godot;

namespace Cutulu
{
    public class EnumCacheHeap<E, V> where E : struct, Enum
    {
        public readonly Dictionary<E, KeyValuePair<short, V>> Data = new();

        public EnumCacheHeap()
        {

        }

        public bool TrySet(E key, V value, short stamp)
        {
            if (TryGetStamp(key, out var _stamp) && _stamp > stamp && Mathf.Abs(_stamp - stamp) < short.MaxValue)
                return false;

            Data[key] = new(stamp, value);
            return true;
        }

        public short Set(E key, V value)
        {
            var stamp = TryGetStamp(key, out var _stamp) ? _stamp : short.MinValue;

            Set(key, value, stamp);
            return stamp;
        }

        public short Set(E key, V value, short stamp)
        {
            Data[key] = new(stamp, value);
            return stamp;
        }

        public short Override(E key, V value)
        {
            var stamp = TryGetStamp(key, out var _stamp) ? _stamp : short.MaxValue;

            Data[key] = new(++stamp, value);
            return stamp;
        }

        public T Get<T>(E key) where T : V
        {
            return TryGet(key, out T t) ? t : default;
        }

        public bool TryGet<T>(E key, out T output) where T : V
        {
            if (Data.TryGetValue(key, out var entry) && entry.Value is T t)
            {
                output = t;
                return true;
            }

            output = default;
            return false;
        }

        public bool TryGetStamp(E key, out short stamp)
        {
            if (Data.TryGetValue(key, out var entry))
            {
                stamp = entry.Key;
                return true;
            }

            stamp = default;
            return false;
        }

        public void Clear()
        {
            Data.Clear();
        }
    }
}