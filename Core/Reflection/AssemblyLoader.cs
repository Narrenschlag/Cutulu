namespace Cutulu.Core
{
    using System.Collections.Concurrent;
    using System.Runtime.Loader;
    using System.Reflection;
    using System.Linq;
    using System.IO;
    using System;

    public static class AssemblyLoader
    {
        private class ManagedLoadContext : AssemblyLoadContext
        {
            public string AssemblyPath { get; }

            public ManagedLoadContext(string assemblyPath) : base(isCollectible: true)
            {
                AssemblyPath = assemblyPath;
            }
        }

        private readonly static ConcurrentDictionary<string, ManagedLoadContext> _loadedContexts = new();

        /// <summary>
        /// Loads an assembly from the specified path or returns an existing context if already loaded.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly file.</param>
        /// <returns>The AssemblyLoadContext containing the assembly.</returns>
        public static AssemblyLoadContext LoadAssembly(string assemblyPath)
        {
            // Ensure the path is absolute
            assemblyPath = Path.GetFullPath(assemblyPath);

            // Check if the assembly is already loaded
            if (_loadedContexts.TryGetValue(assemblyPath, out var existingContext))
            {
                return existingContext;
            }

            // Create a new load context and load the assembly
            var loadContext = new ManagedLoadContext(assemblyPath);
            loadContext.LoadFromAssemblyPath(assemblyPath);

            // Store the load context for future reference
            _loadedContexts[assemblyPath] = loadContext;

            return loadContext;
        }

        /// <summary>
        /// Unloads the assembly and removes it from the manager.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly file to unload.</param>
        public static void UnloadAssembly(string assemblyPath)
        {
            // Ensure the path is absolute
            assemblyPath = Path.GetFullPath(assemblyPath);

            // Check if the assembly is loaded
            if (_loadedContexts.TryRemove(assemblyPath, out var loadContext))
            {
                // Unload the context
                loadContext.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Console.WriteLine($"Unloaded assembly at: {assemblyPath}");
            }
            else
            {
                Console.WriteLine($"Assembly not found for unloading: {assemblyPath}");
            }
        }

        /// <summary>
        /// Gets an already loaded assembly if it exists.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly file.</param>
        /// <returns>The loaded assembly, or null if not loaded.</returns>
        public static Assembly GetLoadedAssembly(string assemblyPath)
        {
            assemblyPath = Path.GetFullPath(assemblyPath);

            if (_loadedContexts.TryGetValue(assemblyPath, out var loadContext))
            {
                return loadContext.Assemblies.FirstOrDefault();
            }

            return null;
        }
    }
}