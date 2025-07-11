#if GODOT4_0_OR_GREATER
namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using System.Linq;

    using Godot;
    using Core;

    public static class ModLoader
    {
        public static readonly Dictionary<IMod, ModInstance> Instances = [];
        public static string[] ModIds { get; set; }

        public static readonly string[] DefaultDirectories = [
            $"{CONST.PROJECT_PATH}Assets/",
            $"{CONST.PROJECT_PATH}Mods/",

            $"{CONST.USER_PATH}Assets/",
            $"{CONST.USER_PATH}Mods/",
        ];

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

            void loadFromDir(string _directory_path)
            {
                if (_directory_path.IsEmpty()) return;

                var _directory = new Directory(_directory_path);

                var _files = _directory.GetSubFiles();
                if (_files.NotEmpty())
                {
                    foreach (var _file in _files)
                    {
                        if (_file.GodotPath.EndsWith(IMod.FileEnding))
                        {
                            var _json = _file.ReadString();
                            if (_json.IsEmpty()) continue;

                            var _mod = _json.json<ExternalMod>();
                            if (_mod != null)
                            {
                                _mod.FilePath = _file.SystemPath;

                                load(_mod).Enabled = enabledByDefault;
                            }

                            else CoreBridge.LogError($"Cannot load mod file at {_file.SystemPath}");
                        }
                    }
                }

                var _directories = _directory.GetSubDirectories();
                if (_directories.NotEmpty())
                {
                    foreach (var _dir in _directories)
                    {
                        loadFromDir(_dir.SystemPath);
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

            return [.. Instances.Values];
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

            GodotPackageLoader.Load(mods);
            CoreBridge.Log($"Loaded godot packages");

            AssetLoader.Load(mods);
            CoreBridge.Log($"Loaded assets");

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
#endif