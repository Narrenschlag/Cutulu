namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using System.Linq;

    using Godot;
    using Core;

    public static class ModLoader
    {
        public static readonly Dictionary<IMod, ModInstance> Instances = new();
        public static string[] ModIds { get; set; }

        public static readonly string[] DefaultDirectories = new[]{
            $"{IO.PROJECT_PATH}Assets/",
            $"{IO.PROJECT_PATH}Mods/",

            $"{IO.USER_PATH}Assets/",
            $"{IO.USER_PATH}Mods/",
        };

        /// <summary>
        /// Find all mods in directories and loads them as instances,
        /// clears old instances
        /// </summary>
        public static ModInstance[] Load(IMod[] alreadyLoaded, string[] directories, bool enabledByDefault = false)
        {
            Unload();

            if (directories.IsEmpty())
                directories = DefaultDirectories;

            if (alreadyLoaded.NotEmpty())
            {
                foreach (var mod in alreadyLoaded)
                {
                    if (mod == null) continue;

                    if (mod is InternalMod internalMod)
                        load(internalMod).Enabled = true;

                    else load(mod).Enabled = enabledByDefault;
                }
            }

            foreach (var directory in directories)
                loadFromDir(directory);

            void loadFromDir(string directory)
            {
                if (directory.IsEmpty()) return;

                var files = IO.GetFiles(directory);

                if (files.NotEmpty())
                {
                    foreach (var file in files)
                    {
                        if (file.ToLower().EndsWith(IMod.FileEnding))
                        {
                            if (IO.TryRead(directory + file, out ExternalMod mod, IO.FileType.Json))
                            {
                                mod.FilePath = directory + file;

                                load(mod).Enabled = enabledByDefault;
                            }

                            else CoreBridge.LogError($"Cannot load mod file at {directory + file}");
                        }
                    }
                }

                var directories = IO.GetDirectories(directory);

                if (directories.NotEmpty())
                {
                    foreach (var dir in directories)
                    {
                        loadFromDir($"{directory}{dir}/");
                    }
                }
            }

            static ModInstance load(IMod mod)
            {
                var instance = new ModInstance(mod);
                Instances[mod] = instance;
                return instance;
            }

            ModIds = new string[Instances.Count];
            var i = 0;

            foreach (var mod in Instances.Keys)
                ModIds[i++] = mod.ID;

            return Instances.Values.ToArray();
        }

        /// <summary>
        /// Clear loaded mods
        /// </summary>
        public static void Unload()
        {
            Deactivate();

            foreach (var instance in Instances.Values)
            {
                instance.Unload();
            }

            Instances.Clear();
            ModIds = null;
        }

        /// <summary>
        /// Activates all enabled mods
        /// </summary>
        public static void Activate()
        {
            Deactivate();

            if (Instances.IsEmpty()) return;

            // Order instances by load order
            var sortedInstances = Instances.Values.OrderBy(x => x.LoadOrder).ToArray();

            var list = new List<IMod>();
            foreach (var instance in sortedInstances)
            {
                if (instance.Enabled)
                {
                    // Check for dependencies
                    if (instance.Source.Dependencies.NotEmpty())
                    {
                        var dependenciesMet = true;

                        foreach (var dependency in instance.Source.Dependencies)
                        {
                            if (ModIds.Contains(dependency) == false)
                            {
                                dependenciesMet = false;
                                break;
                            }
                        }

                        if (dependenciesMet == false)
                        {
                            CoreBridge.LogError($"Mod {instance.Source.Name} depends on mod(s) {instance.Source.Dependencies.Join(", ")} but one or more are not loaded.");
                            continue;
                        }
                    }

                    list.Add(instance.Source);
                }
            }

            var mods = list.ToArray();

            CoreBridge.Log($"Activating {mods.Size()}/{Instances.Count} mods");

            AssemblyLoader.Load(mods);
            CoreBridge.Log($"Loaded assemblies");

            AssetLoader.Load(mods);
            CoreBridge.Log($"Loaded assets");

            GodotPackageLoader.Load(mods);
            CoreBridge.Log($"Loaded godot packages");

            foreach (var mod in mods)
            {
                Instances[mod].Activate();
            }
        }

        /// <summary>
        /// Deactivates all active mods
        /// </summary>
        public static void Deactivate()
        {
            foreach (var instance in Instances.Values)
            {
                if (instance.Active)
                    instance.Deactivate();
            }

            AssemblyLoader.Unload();
            AssetLoader.Unload();
        }
    }
}