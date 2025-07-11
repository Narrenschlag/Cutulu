#if GODOT4_0_OR_GREATER
namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using System;

    using Godot;
    using Core;

    public class AssetInstance
    {
        public readonly Dictionary<string, string> References = [];

        public readonly IMod Source;

        public AssetInstance(IMod source)
        {
            Source = source;

            Register();
        }

        private void Register()
        {
            var entries = Source.ReadAssetEntries();

            if (entries.IsEmpty()) return;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Path.PathExists() == false) CoreBridge.LogError($"Asset at path '{entries[i].Path}' does not exist.");

                // Register direct address
                else References[entries[i].Name] = entries[i].Path;
            }
        }

        public bool TryGet<T>(string name, out T value) where T : class
        {
            object obj = null;

            try
            {
                switch (typeof(T))
                {
                    case Type i when i == typeof(string):
                        obj = new File(References[name]).ReadString();
                        break;

                    case Type i when i == typeof(byte[]):
                        obj = new File(References[name]).Read();
                        break;

                    case Type i when i.IsSubclassOf(typeof(Resource)):
                        obj = ResourceLoader.Load(References[name], typeof(T).Name);
                        break;

                    default:
                        Debug.LogError($"Cannot read typeof({typeof(T).Name})");
                        break;
                }
            }
            catch { }

            if (obj is T t && t != null && (t is not Node n || n.NotNull()))
            {
                value = t;
                return true;
            }

            Debug.LogError($"Cant read typeof({typeof(T).Name}) from {name} @'{References[name]}' {obj != null}");
            value = default;
            return false;
        }
    }
}
#endif