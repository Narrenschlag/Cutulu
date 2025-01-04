using System.Collections.Generic;
using System.Linq;

namespace Cutulu.Core
{
    public class UniqueDictionary<Key, Value>
    {
        public readonly Dictionary<Value, Key> Reverse = new();
        public readonly Dictionary<Key, Value> Index = new();

        public int Length => Index.Count;
        public int Count => Index.Count;
        public int Size => Index.Count;

        public Value LastValue => Index.Values.Last();
        public Key LastKey => Index.Keys.Last();

        public Value FirstValue => Index.Values.First();
        public Key FirstKey => Index.Keys.First();

        public Value RandomValue => Index.Values.ElementAt(Random.Range(Length));
        public Key RandomKey => Index.Keys.ElementAt(Random.Range(Length));

        public void Add(Value value, Key key) => Add(key, value);
        public void Add(Key key, Value value)
        {
            Reverse.Add(value, key);
            Index.Add(key, value);
        }

        public void Remove(Key key)
        {
            if (TryGet(key, out var value))
            {
                Reverse.Remove(value);
                Index.Remove(key);
            }
        }

        public void Remove(Value value)
        {
            if (TryGet(value, out var key))
            {
                Reverse.Remove(value);
                Index.Remove(key);
            }
        }

        public bool TryGet(Value value, out Key key) => Reverse.TryGetValue(value, out key);
        public bool TryGet(Key key, out Value value) => Index.TryGetValue(key, out value);

        public bool Contains(Value value) => Reverse.ContainsKey(value);
        public bool Contains(Key key) => Index.ContainsKey(key);

        public Key Get(Value value) => Reverse[value];
        public Value Get(Key key) => Index[key];

        public void Clear()
        {
            Reverse.Clear();
            Index.Clear();
        }
    }
}