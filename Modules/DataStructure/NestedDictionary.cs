using System.Collections.Generic;

namespace Cutulu
{
    public struct Dictionary<TRootKey, TKey, TValue>
    {
        private Dictionary<TRootKey, Dictionary<TKey, TValue>> dic;

        public Dictionary()
        {
            dic = new Dictionary<TRootKey, Dictionary<TKey, TValue>>();
        }

        public void Add(TRootKey rootKey, TKey key, TValue value)
        {
            if (dic == null) dic = new Dictionary<TRootKey, Dictionary<TKey, TValue>>();
            if (!ContainsKey(rootKey)) dic.Add(rootKey, new Dictionary<TKey, TValue>());

            dic[rootKey].Add(key, value);
        }

        public TValue this[TRootKey rootKey, TKey key]
        {
            set => dic[rootKey][key] = value;
            get => dic[rootKey][key];
        }

        public bool TryGetValue(TRootKey rootKey, out Dictionary<TKey, TValue> value) => dic.TryGetValue(rootKey, out value);
        public bool TryGetValue(TRootKey rootKey, TKey key, out TValue value)
        {
            if (ContainsKey(rootKey, key))
            {
                value = dic[rootKey][key];
                return true;
            }

            value = default(TValue);
            return false;
        }

        public bool ContainsKey(TRootKey rootKey, TKey key) => ContainsKey(rootKey) && dic[rootKey].ContainsKey(key);
        public bool ContainsKey(TRootKey rootKey) => dic.IsEmpty() ? false : dic.ContainsKey(rootKey);

        public void Remove(TRootKey rootKey, TKey key) => dic[rootKey].Remove(key);
        public void Remove(TRootKey rootKey) => dic.Remove(rootKey);
    }
}