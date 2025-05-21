using System.Collections.Generic;

namespace Cutulu.Core
{
    public readonly struct Dictionary<TRootKey, TKey, TValue>
    {
        private readonly Dictionary<TRootKey, Dictionary<TKey, TValue>> Dic;

        public Dictionary()
        {
            Dic = [];
        }

        public readonly void Add(TRootKey rootKey, TKey key, TValue value)
        {
            if (!ContainsKey(rootKey)) Dic.Add(rootKey, []);

            Dic[rootKey][key] = value;
        }

        public readonly TValue this[TRootKey rootKey, TKey key]
        {
            set => Dic[rootKey][key] = value;
            get => Dic[rootKey][key];
        }

        public readonly bool TryGetValue(TRootKey rootKey, out Dictionary<TKey, TValue> value) => Dic.TryGetValue(rootKey, out value);
        public readonly bool TryGetValue(TRootKey rootKey, TKey key, out TValue value)
        {
            if (ContainsKey(rootKey, key))
            {
                value = Dic[rootKey][key];
                return true;
            }

            value = default;
            return false;
        }

        public readonly bool ContainsKey(TRootKey rootKey, TKey key) => ContainsKey(rootKey) && Dic[rootKey].ContainsKey(key);
        public readonly bool ContainsKey(TRootKey rootKey) => !Dic.IsEmpty() && Dic.ContainsKey(rootKey);

        public readonly void Remove(TRootKey rootKey, TKey key) => Dic[rootKey].Remove(key);
        public readonly void Remove(TRootKey rootKey) => Dic.Remove(rootKey);

        public readonly bool IsNull() => Dic == null;
        public readonly bool NotNull() => !IsNull();

        public readonly bool IsEmpty() => IsNull() || Dic.IsEmpty();
        public readonly bool NotEmpty() => !IsEmpty();
    }
}