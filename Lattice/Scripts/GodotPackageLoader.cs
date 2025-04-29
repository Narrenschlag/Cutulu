namespace Cutulu.Lattice
{
    using System.Collections.Generic;

    using Core;

    public static class GodotPackageLoader
    {
        public static readonly List<IMod> Instances = new();

        /// <summary>
        /// Register godot packages
        /// </summary>
        public static void Load(params IMod[] mods)
        {
            if (mods.IsEmpty()) return;

            foreach (var mod in mods)
            {
                if (Instances.Contains(mod)) continue;

                var entries = mod.ReadPackageEntries();
                if (entries.IsEmpty()) continue;

                foreach (var entry in entries)
                {
                    if (entry.Path.ToLower().EndsWith(".pck") && entry.Path.PathExists())
                        Godot.ProjectSettings.LoadResourcePack(entry.Path);
                }
            }
        }
    }
}