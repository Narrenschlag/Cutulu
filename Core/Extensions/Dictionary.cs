namespace Cutulu.Core
{
    using System.Collections.Generic;

    public static class DictionaryExtension
    {
        public static bool NotEmpty<T, U>(this Dictionary<T, U> dic) => dic != null && dic.Count > 0;
        public static bool IsEmpty<T, U>(this Dictionary<T, U> dic) => !NotEmpty(dic);

        /// <summary>
        /// Sets entry no matter if key is already contained.
        /// </summary>
        public static void Set<K, V>(this Dictionary<K, V> dic, K key, V value)
        {
            if ((dic ??= new()).ContainsKey(key))
            {
                dic[key] = value;
            }

            else
            {
                dic.Add(key, value);
            }
        }

        /// <summary>
        /// Adds key, value to dictionary if not contained.
        /// </summary>
        public static bool TryAdd<K, V>(this Dictionary<K, V> dic, K key, V value)
        {
            if ((dic ??= new()).ContainsKey(key))
            {
                return false;
            }

            else
            {
                dic.Add(key, value);
                return true;
            }
        }

        /// <summary>
        /// Removes key from dictionary if contained.
        /// </summary>
        public static bool TryRemove<K, V>(this Dictionary<K, V> dic, K key)
        {
            if ((dic ??= new()).ContainsKey(key) == false)
            {
                return false;
            }

            else
            {
                dic.Remove(key);
                return true;
            }
        }
    }
}