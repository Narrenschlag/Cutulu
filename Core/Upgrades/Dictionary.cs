namespace Cutulu.Core.Upgrades
{
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// A mighty collection of key-value pairs, worthy of the finest libraries of Rivendell.
    /// Implements various interfaces to ensure it can be used in many scenarios.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    public class Dictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue> where TKey : notnull
    {
        // Events to notify when keys are added or removed, much like the tales of old.
        public event Action<Dictionary<TKey, TValue>, TKey, TValue> AddedEntry, RemovedEntry, UpdatedEntry;
        public event Action<Dictionary<TKey, TValue>> Cleared, Changed;

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                if (ContainsKey(key))
                {
                    base[key] = value;

                    UpdatedEntry?.Invoke(this, key, value);
                    Changed?.Invoke(this);
                }

                else Add(key, value);
            }
        }

        // Checks if the dictionary contains a specific key-value pair.
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        // Copies the elements to an array, like passing on wisdom from one generation to the next.
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(array, arrayIndex);
        }

        // Adds a key-value pair to the dictionary.
        public bool Add(KeyValuePair<TKey, TValue> item)
        {
            return Add(item.Key, item.Value);
        }

        // Adds a key-value pair to the dictionary directly.
        public new bool Add(TKey key, TValue value)
        {
            if (base.ContainsKey(key) == false)
            {
                base.Add(key, value);

                AddedEntry?.Invoke(this, key, value);
                Changed?.Invoke(this);

                return true;
            }

            return false;
        }

        // Removes a key-value pair from the dictionary.
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
            {
                Remove(item.Key);
                return true;
            }

            return false;
        }

        // Removes a key-value pair from the dictionary directly.
        public new bool Remove(TKey key)
        {
            if (base.TryGetValue(key, out var value))
            {
                base.Remove(key);

                RemovedEntry?.Invoke(this, key, value);
                Changed?.Invoke(this);

                return true;
            }

            return false;
        }

        // Clears all entries from the dictionary, much like cleansing a battlefield.
        public new bool Clear()
        {
            if (Count > 0)
            {
                base.Clear();
                Cleared?.Invoke(this);
                Changed?.Invoke(this);

                return true;
            }

            return false;
        }
    }
}
