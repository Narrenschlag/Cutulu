namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using System.Reflection;
    using System;

    using Cutulu.Core;

    public class ModWeaver
    {
        private readonly Dictionary<string, ModLoadContext> _modContexts = new();

        public void LoadMod(string filePath)
        {
            var modFiles = System.IO.Directory.GetFiles(filePath, "*.dll");
            foreach (var p in modFiles)
            {
                try
                {
                    LoadAssembly(p);
                    CoreBridge.Log($"Mod {p} loaded successfully.");
                }

                catch (Exception ex)
                {
                    CoreBridge.LogError($"Failed to load mod {p}: {ex.Message}");
                }
            }
        }

        private Assembly LoadAssembly(string filePath)
        {
            var assembly = AssemblyLoader.GetAssembly(filePath);
            if (assembly != null) return assembly;

            var modContext = new ModLoadContext();
            var loadedAssembly = modContext.LoadAssembly(filePath);

            _modContexts[filePath] = modContext;
            return loadedAssembly;
        }

        public void UnloadMod(string filePath)
        {
            if (_modContexts.TryGetValue(filePath, out var context))
            {
                context.Unload();
                _modContexts.Remove(filePath);

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}