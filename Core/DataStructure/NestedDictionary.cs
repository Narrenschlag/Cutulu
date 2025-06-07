using System.Collections.Generic;

namespace Cutulu.Core
{
    public class Dictionary<TRootKey, TKey, TValue> : Dictionary<TRootKey, Dictionary<TKey, TValue>>
    {
        public TValue this[TRootKey rootKey, TKey key]
        {
            get => TryGetValue(rootKey, key, out var value) ? value : default;
            set => Add(rootKey, key, value);
        }

        public void Add(TRootKey rootKey, TKey key, TValue value)
        {
            if (!ContainsKey(rootKey)) Add(rootKey, []);

            this[rootKey][key] = value;
        }

        public bool TryGetValue(TRootKey rootKey, TKey key, out TValue value)
        {
            if (ContainsKey(rootKey, key))
            {
                value = this[rootKey][key];
                return true;
            }

            value = default;
            return false;
        }

        public bool ContainsKey(TRootKey rootKey, TKey key) => ContainsKey(rootKey) && this[rootKey].ContainsKey(key);

        public void Remove(TRootKey rootKey, TKey key) => this[rootKey].Remove(key);
    }
}