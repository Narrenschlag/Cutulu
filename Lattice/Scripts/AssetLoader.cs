namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using Cutulu.Core;
    using Godot;

    public static class AssetLoader
    {
        public static readonly Dictionary<string, AssetInstance> References = new();
        public static readonly Dictionary<string, List<string>> Collections = new();

        public static readonly Dictionary<IMod, AssetInstance> Instances = new();
        public static readonly Dictionary<string, object> Cache = new();

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

                foreach (var name in instance.References.Keys)
                {
                    // Find all collections
                    var args = name.TrimEndUntil('/', '\\').Split(new[] { '/', '\\' }, Constant.StringSplit);

                    // Register collections
                    if (args.Size() >= 2)
                    {
                        var dir = "";

                        for (int j = 0; j < args.Length - 1; j++)
                        {
                            if (Collections.TryGetValue(args[j], out var set) == false)
                                Collections[args[j]] = set = new();

                            if (j > 0) dir += '/';
                            set.TryAdd(dir += args[j + 1]);
                        }
                    }

                    References[name] = instance;
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

            if (Collections.TryGetValue(collection, out var set))
            {
                if (Cache.TryGetValue(collection, out var val) && val is T[] t)
                {
                    values = t;
                    return true;
                }

                foreach (var name in set)
                {
                    if (TryGet(name, out T _t))
                        list.Add(_t);
                }

                Cache[collection] = values = list.ToArray();
            }

            else values = System.Array.Empty<T>();

            return values.NotEmpty();
        }

        /// <summary>
        /// Returns asset
        /// </summary>
        public static bool TryGet<T>(string name, out T value) where T : class
        {
            if (Cache.TryGetValue(name, out var val) && val is T t)
            {
                value = t;
                return true;
            }

            if (References.TryGetValue(name, out var instance))
            {
                return instance.TryGet(name, out value);
            }

            value = default;
            return false;
        }
    }
}