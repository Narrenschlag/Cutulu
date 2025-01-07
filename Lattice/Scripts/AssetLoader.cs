namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using Cutulu.Core;
    using Godot;

    public static class AssetLoader
    {
        public static readonly Dictionary<string, List<string>> Collections = new();
        public static readonly Dictionary<string, string> Direct = new();

        public static readonly Dictionary<string, object> Cache = new();

        public static void Register(IMod mod)
        {
            var entries = mod.ReadAssetEntries();

            if (entries.IsEmpty()) return;

            for (int i = 0; i < entries.Length; i++)
            {
                if (IO.Exists(entries[i].Path) == false) CoreBridge.LogError($"Asset at path '{entries[i].Path}' does not exist.");
                else
                {
                    // Find all collections
                    var args = entries[i].Name.TrimEndUntil('/', '\\').Split(new[] { '/', '\\' }, Constant.StringSplit);

                    // Register collections
                    if (args.Size() >= 2)
                    {
                        var dir = "";

                        for (int j = 0; j < args.Length - 1; j++)
                        {
                            if (Collections.TryGetValue(args[j], out var set) == false)
                                Collections[args[j]] = set = new();

                            if (j > 0) dir += '/';
                            set.Add(dir += args[j + 1]);
                        }
                    }

                    // Register direct address
                    Direct[entries[i].Name] = entries[i].Path;
                }
            }
        }

        public static void Clear()
        {
            Collections.Clear();
            Direct.Clear();

            Cache.Clear();
        }

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

        public static bool TryGet<T>(string name, out T value) where T : class
        {
            if (Cache.TryGetValue(name, out var val) && val is T t)
            {
                value = t;
                return true;
            }

            switch (value = default)
            {
                case string _:
                    Cache[name] = value = (T)(object)IO.ReadString(Direct[name]);
                    return true;

                case byte[] _:
                    Cache[name] = value = (T)(object)IO.ReadBytes(Direct[name]);
                    return true;

                case Resource _:
                    Cache[name] = value = (T)(object)ResourceLoader.Load(Direct[name], typeof(T).Name);
                    return true;
            }

            return false;
        }
    }
}