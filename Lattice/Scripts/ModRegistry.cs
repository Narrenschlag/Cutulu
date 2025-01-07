namespace Cutulu.Lattice
{
    using System.Collections.Generic;

    public static class ModRegistry
    {
        private static readonly Dictionary<string, IMod> Mods = new();

        public static void RegisterMod(IMod mod)
        {
            if (Mods.ContainsKey(mod.Name))
            {
                CoreBridge.LogError($"Mod {mod.Name} is already registered.");
                return;
            }

            Mods[mod.Name] = mod;
        }

        public static IMod GetMod(string name)
        {
            return Mods.TryGetValue(name, out var mod) ? mod : null;
        }

        public static ICollection<IMod> GetAllMods()
        {
            return Mods.Values;
        }
    }
}