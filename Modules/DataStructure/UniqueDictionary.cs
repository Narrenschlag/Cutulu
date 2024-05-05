using System.Collections.Generic;
using System.Linq;

namespace Cutulu
{
    public readonly struct UniqueDictionary<Key, Value>
    {
        public readonly Dictionary<Value, Key> Reverse;
        public readonly Dictionary<Key, Value> Index;

        public int Length => Index.Count;
        public int Count => Index.Count;
        public int Size => Index.Count;

        public Value LastValue => Index.Values.Last();
        public Key LastKey => Index.Keys.Last();

        public UniqueDictionary()
        {
            Reverse = new();
            Index = new();
        }

        public readonly void Add(Value value, Key key) => Add(key, value);
        public readonly void Add(Key key, Value value)
        {
            Reverse.Add(value, key);
            Index.Add(key, value);
        }

        public readonly void Remove(Key key)
        {
            if (TryGet(key, out var value))
            {
                Reverse.Remove(value);
                Index.Remove(key);
            }
        }

        public readonly void Remove(Value value)
        {
            if (TryGet(value, out var key))
            {
                Reverse.Remove(value);
                Index.Remove(key);
            }
        }

        public readonly bool TryGet(Value value, out Key key) => Reverse.TryGetValue(value, out key);
        public readonly bool TryGet(Key key, out Value value) => Index.TryGetValue(key, out value);

        public readonly bool Contains(Value value) => Reverse.ContainsKey(value);
        public readonly bool Contains(Key key) => Index.ContainsKey(key);

        public readonly Key Get(Value value) => Reverse[value];
        public readonly Value Get(Key key) => Index[key];
    }
}