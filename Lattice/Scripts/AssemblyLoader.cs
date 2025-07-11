#if GODOT4_0_OR_GREATER
namespace Cutulu.Lattice
{
    using System.Collections.Generic;

    using Core;

    public static class AssemblyLoader
    {
        public static readonly Dictionary<IMod, AssemblyInstance> Instances = new();
        public static readonly List<string> Assemblies = new();

        /// <summary>
        /// Register assemblies
        /// </summary>
        public static void Load(params IMod[] mods)
        {
            if (mods.IsEmpty()) return;

            foreach (var mod in mods)
            {
                if (Instances.TryGetValue(mod, out var instance) && instance != null) continue;

                Instances[mod] = instance = new(mod);

                foreach (var entry in instance.Entries)
                {
                    if (Assemblies.Contains(entry))
                    {
                        Core.AssemblyLoader.LoadAssembly(entry);
                        Assemblies.Add(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Clear assembly
        /// </summary>
        public static void Unload()
        {
            foreach (var assembly in Assemblies)
            {
                Core.AssemblyLoader.UnloadAssembly(assembly);
            }

            Assemblies.Clear();
            Instances.Clear();
        }
    }
}
#endif