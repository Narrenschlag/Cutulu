#if GODOT4_0_OR_GREATER
namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using System;

    using Godot;
    using Core;

    public static class AssetLoader
    {
        public static readonly Dictionary<string, AssetInstance> References = [];
        public static readonly Dictionary<string, List<string>> Collections = [];

        public static readonly Dictionary<string, Dictionary<Type, object>> Cache = [];
        public static readonly Dictionary<IMod, AssetInstance> Instances = [];
        private static readonly char[] separator = ['/', '\\'];

        /// <summary>
        /// Register assets
        /// </summary>
        public static void Load(params IMod[] mods)
        {
            if (mods.IsEmpty()) return;

            foreach (var mod in mods)
            {
                if (Instances.TryGetValue(mod, out var instance) || instance == null)
                    Instances[mod] = instance = new(mod);

                foreach (var pair in instance.References)
                {
                    // Check if it is a collection
                    if (pair.Key.Contains('/') || pair.Key.Contains('\\'))
                    {
                        // Find all collections
                        var args = pair.Key.TrimEndUntil('/', '\\').Split(separator, CONST.StringSplit);

                        // Register collections
                        if (args.Size() > 0)
                        {
                            var dir = "";

                            for (int j = 0; j < args.Length; j++)
                            {
                                if (j > 0) dir += '/';
                                dir += args[j];

                                if (Collections.TryGetValue(dir, out var references) == false)
                                {
                                    Collections[dir] = references = [];
                                }

                                references.TryAdd(pair.Key);
                            }
                        }
                    }

                    References[pair.Key] = instance;
                }
            }
        }

        /// <summary>
        /// Clear entries
        /// </summary>
        public static void Unload()
        {
            Collections.Clear();
            References.Clear();

            Instances.Clear();
            Cache.Clear();
        }

        /// <summary>
        /// Returns collection of assets
        /// </summary>
        public static bool TryGet<T>(string collection, out T[] values) where T : class
        {
            var list = new List<T>();

            if (Collections.TryGetValue(collection, out var references))
            {
                // Try get from cache
                if (Cache.TryGetValue(collection, out var dictionary) && dictionary.TryGetValue(typeof(T), out var val) && val is T[] t)
                {
                    values = t;
                    return true;
                }

                // Get references
                foreach (var name in references)
                {
                    if (TryGet(name, out T _t))
                        list.Add(_t);
                }

                values = [.. list];

                // Assign to cache
                if (values.NotEmpty())
                {
                    if (Cache.TryGetValue(collection, out dictionary) == false)
                        Cache[collection] = dictionary = [];

                    dictionary[typeof(T)] = values;
                }
            }

            else values = [];

            return values.NotEmpty();
        }

        /// <summary>
        /// Returns asset
        /// </summary>
        public static bool TryGet<T>(string name, out T value) where T : class
        {
            // Try get from cache
            if (Cache.TryGetValue(name, out var dictionary))
            {
                if (dictionary.TryGetValue(typeof(T), out var val) && val is T t)
                {
                    value = t;
                    return true;
                }
            }

            // Assign to cache
            if (References.TryGetValue(name, out var instance) && instance.TryGet(name, out value))
            {
                if (Cache.TryGetValue(name, out dictionary) == false)
                    Cache[name] = dictionary = [];

                dictionary[typeof(T)] = value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns asset, if not exstant returns default value
        /// </summary>
        public static T Get<T>(string name, T defaultValue = default) where T : class
        {
            return TryGet(name, out T value) ? value : defaultValue;
        }

        /// <summary>
        /// Returns source of asset if exists
        /// </summary>
        public static bool TryGetSource(string name, out IMod source)
        {
            if (References.TryGetValue(name, out var instance))
            {
                source = instance.Source;
                return true;
            }

            source = null;
            return false;
        }
    }
}
#endif