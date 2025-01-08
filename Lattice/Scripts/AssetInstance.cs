namespace Cutulu.Lattice
{
    using System.Collections.Generic;

    using Godot;
    using Core;

    public class AssetInstance
    {
        public readonly Dictionary<string, string> References = new();

        private readonly IMod Source;

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
                if (IO.Exists(entries[i].Path) == false) CoreBridge.LogError($"Asset at path '{entries[i].Path}' does not exist.");

                // Register direct address
                else References[entries[i].Name] = entries[i].Path;
            }
        }

        public bool TryGet<T>(string name, out T value) where T : class
        {
            switch (value = default)
            {
                case string _:
                    value = (T)(object)IO.ReadString(References[name]);
                    return true;

                case byte[] _:
                    value = (T)(object)IO.ReadBytes(References[name]);
                    return true;

                case Resource _:
                    value = (T)(object)ResourceLoader.Load(References[name], typeof(T).Name);
                    return true;
            }

            return false;
        }
    }
}