namespace Cutulu.Core
{
    using System.Collections.Generic;

    public static class Dictionaryf
    {
        public static bool NotEmpty<T, U>(this Dictionary<T, U> dic) => dic != null && dic.Count > 0;
        public static bool IsEmpty<T, U>(this Dictionary<T, U> dic) => !NotEmpty(dic);

        /// <summary>
        /// Removes null keys from dictionary.
        /// </summary>
        public static Dictionary<K, V> ClearNullKeys<K, V>(this Dictionary<K, V> dic)
        {
            if (dic == null) return null;

            var keys = new K[dic.Count];
            dic.Keys.CopyTo(keys, 0);

            foreach (var key in keys)
                if (key.IsNull())
                    dic.Remove(key);

            return dic;
        }

        /// <summary>
        /// Sets entry no matter if key is already contained.
        /// </summary>
        public static void Set<K, V>(this Dictionary<K, V> dic, K key, V value)
        {
            if ((dic ??= []).ContainsKey(key))
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
            if ((dic ??= []).ContainsKey(key))
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
            if ((dic ??= []).ContainsKey(key) == false)
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