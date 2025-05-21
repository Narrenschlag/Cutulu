namespace Cutulu.Core
{
    using System.Collections.Generic;
    using Godot;

    public class CacheHeap<V>
    {
        public readonly Dictionary<int, KeyValuePair<short, V>> Data = [];

        public CacheHeap()
        {

        }

        public int Count => Data.Count;

        public static bool CanOverride(int oldStamp, int newStamp) => Mathf.Abs(oldStamp - newStamp) > short.MaxValue || newStamp > oldStamp;

        public bool TrySet(int key, V value, short stamp)
        {
            if (TryGetStamp(key, out var _stamp) && CanOverride(_stamp, stamp) == false)
            {
                return false;
            }

            Data[key] = new(stamp, value);
            return true;
        }

        public short Set(int key, V value)
        {
            var stamp = TryGetStamp(key, out var _stamp) ? _stamp : short.MinValue;

            Set(key, value, stamp);
            return stamp;
        }

        public short Set(int key, V value, short stamp)
        {
            Data[key] = new(stamp, value);
            return stamp;
        }

        public short Override(int key, V value)
        {
            var stamp = TryGetStamp(key, out var _stamp) ? _stamp : short.MaxValue;

            Data[key] = new(++stamp, value);
            return stamp;
        }

        public T Get<T>(int key) where T : V
        {
            return TryGet(key, out T t) ? t : default;
        }

        public bool Contains(int key)
        {
            return Data.ContainsKey(key);
        }

        public bool TryGet<T>(int key, out T output) where T : V
        {
            if (Data.TryGetValue(key, out var entry) && entry.Value is T t)
            {
                output = t;
                return true;
            }

            output = default;
            return false;
        }

        public bool TryGetStamp(int key, out short stamp)
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