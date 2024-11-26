namespace Cutulu
{
    using System.Collections.Generic;
    using System.Reflection;
    using System;
    using Godot;

    public partial class AssetLibrary
    {
        public readonly Dictionary<string, AssetBook> LoadedMods = new();
        public string[] RootDirectories { get; private set; }

        public readonly Dictionary<string, KeyValuePair<AssetBook, string>> GlobalAddresses = new();
        public readonly Dictionary<string, List<string>> IdDirectories = new();
        private readonly Dictionary<string, object> LoadedData = new();

        /// <summary>
        /// Loads mods from given root directories and enables them
        /// <summar>
        public AssetLibrary(params string[] root) : this(true, root) { }

        /// <summary>
        /// Loads mods from given root directories
        /// <summar>
        public AssetLibrary(bool enableAll, params string[] root)
        {
            LoadMods(root);

            if (enableAll)
                EnableAll();
        }

        /// <summary>
        /// Loads mods from given root directories
        /// <summar>
        public void LoadMods(params string[] root)
        {
            // Clear mods
            LoadedMods.Clear();

            // Replace local root directories with previously loaded root directories if empty
            if (root.IsEmpty())
                root = RootDirectories;

            // Return if root directories are empty
            if (root.IsEmpty()) return;
            RootDirectories = root;

            // Iterate through every root directory
            foreach (var directory in root)
            {
                var filePaths = new List<string>();

                // Find all mod data file paths
                OE.FindFiles(directory, ref filePaths, new[] { AssetConstants.MOD_ENDING }); //, new[] { ".zip" }); // Disabled .zip because of dll/pck file loading

                // Iterate through mod data files
                foreach (var filePath in filePaths)
                {
                    // Ignore sample file
                    if (filePath.EndsWith("/sample.cm")) continue;

                    // Load mod from file path
                    LoadMod(filePath);
                }
            }
        }

        /// <summary>
        /// Loads single mod from given path
        /// <summar>
        public void LoadMod(string filePath)
        {
            try
            {
                var loaded = new AssetBook(this, filePath);

                if (LoadedMods.ContainsKey(loaded.Data.Id))
                    throw new Exception($"Mod with id '{loaded.Data.Id}' has already been added. Cannot load multiple mods with the same id.");

                LoadedMods[loaded.Data.Id] = loaded;
                Debug.LogR($"[color=seagreen]Loaded [b]{loaded.Data.Name}[/b] by [b]{loaded.Data.Author}[/b] [i]({loaded.Data.Id}, {loaded.Data.Version}, {loaded.FilePath})[/i]");
            }

            catch (Exception exception)
            {
                Debug.LogError($"Loading mod at '{filePath}' failed. ({exception.Message})\n{exception.StackTrace}");
            }
        }

        /// <summary>
        /// Refreshes global entries based on loaded and enabled mods
        /// <summar>
        public void Refresh()
        {
            GlobalAddresses.Clear();
            IdDirectories.Clear();
            LoadedData.Clear();

            foreach (var loaded in LoadedMods.Values)
            {
                // Ignore disabled mods
                if (loaded.Enabled == false) continue;

                string fixPath(string local) => $"{loaded.RootDirectory}{local}";

                // Load all dll files
                if (loaded.Data.DllPaths.NotEmpty())
                {
                    foreach (var dllPath in loaded.Data.DllPaths)
                    {
                        var path = fixPath(dllPath);
                        if (LoadDll(path) == false) continue;
                        Debug.LogR($"[color={Colors.PaleVioletRed.ToHtml()}]Loaded '{path}'");
                    }
                }

                // Load all pck files
                if (loaded.Data.PckPaths.NotEmpty())
                {
                    foreach (var pckPath in loaded.Data.PckPaths)
                    {
                        var path = fixPath(pckPath);
                        if (LoadPck(path) == false) continue;
                        Debug.LogR($"[color={Colors.PaleGreen.ToHtml()}]Loaded '{path}'");
                    }
                }

                foreach (var loadedPair in loaded.Addresses)
                {
                    var key = loadedPair.Key;

                    // Handle priority
                    if (GlobalAddresses.TryGetValue(key, out var pair))
                    {
                        if (pair.Key.Priority >= loaded.Priority) continue;
                    }

                    // Add address to global directories
                    else
                    {
                        // Add dictionaries with depth for targetting specific types
                        var directorySplits = key.Split(AssetConstants.NAME_SEPERATOR, Cutulu.Constants.StringSplit);
                        var strng = new System.Text.StringBuilder();

                        if (directorySplits.Size() > 1)
                        {
                            // Iterate through every directory split
                            for (int k = 0; k < directorySplits.Length - 1; k++)
                            {
                                // Clear string builder
                                strng.Clear();

                                // Finalize directory name
                                for (int j = 0; j <= k; j++)
                                {
                                    if (j > 0) strng.Append('/');
                                    strng.Append(directorySplits[j]);
                                }

                                // Get directory and validate it
                                var dir = strng.ToString();
                                if (dir.IsEmpty()) continue;

                                // Check for empty entries and add if needed
                                if (IdDirectories.TryGetValue(dir, out var set) == false)
                                {
                                    IdDirectories[dir] = set = new();
                                }

                                // Try adding key
                                set.TryAdd(key);
                            }
                        }
                    }

                    // Global address for the name
                    GlobalAddresses[key] = new(loaded, loadedPair.Value[0]);
                }
            }

            Debug.LogR($"[color=seagreen]Loaded {GlobalAddresses.Count} assets");
        }

        /// <summary>
        /// Enables all loaded mods
        /// <summar>
        public void EnableAll()
        {
            foreach (var loaded in LoadedMods.Values)
            {
                loaded.Enable(false);
            }

            Refresh();
        }

        /// <summary>
        /// Overwrites priority if modId is registered
        /// <summar>
        public bool TryAssignPriority(string modId, int priority)
        {
            if (LoadedMods.TryGetValue(modId, out var loaded) == false) return false;

            loaded.Priority = priority;
            return true;
        }

        /// <summary>
        /// Enables mod if modId is registered
        /// <summar>
        public bool TryEnable(string modId, bool refresh = true)
        {
            if (LoadedMods.TryGetValue(modId, out var loaded) == false) return false;

            loaded.Enable(refresh);
            return true;
        }

        /// <summary>
        /// Disables all loaded mods
        /// <summar>
        public void DisableAll()
        {
            foreach (var loaded in LoadedMods.Values)
            {
                loaded.Disable(false);
            }

            Refresh();
        }

        /// <summary>
        /// Disables mod if modId is registered
        /// <summar>
        public bool TryDisable(string modId, bool refresh = true)
        {
            if (LoadedMods.TryGetValue(modId, out var loaded) == false) return false;

            loaded.Disable(refresh);
            return true;
        }

        #region Read Data

        public T Get<T>(string identifier) where T : class
        {
            return TryGet(identifier, out T t) ? t : default;
        }

        public bool TryGet<T>(string identifier, out T output) where T : class
        {
            if (typeof(T).IsSubclassOf(typeof(Godot.Resource))) output = GetResource<T>(identifier);

            else output = GetNonResource<T>(identifier);

            return output is not null;
        }

        /// <summary>
        /// Returns resource of given resource type. Checks for null references.
        /// </summary>
        public T GetResource<T>(string assetName) where T : class
        {
            if (assetName.IsEmpty()) return default;

            if (LoadedData.TryGetValue(assetName, out var output) == false || output is not T)
            {
                if (GlobalAddresses.TryGetValue(assetName, out var path))
                {
                    output = ResourceLoader.Load<T>(path.Value);
                }

                if (output is T && output is Godot.Resource _output) LoadedData[assetName] = _output;
            }

            return output is T t ? t : default;
        }

        /// <summary>
        /// Returns non-resource of given non-resource type. Checks for null references.
        /// </summary>
        public T GetNonResource<T>(string assetName, IO.FileType type = IO.FileType.Binary)
        {
            if (assetName.IsEmpty()) return default;

            if (LoadedData.TryGetValue(assetName, out var nonResource) == false || nonResource == null)
            {
                if (GlobalAddresses.TryGetValue(assetName, out var path) && OE.TryGetData(path.Value, out T loaded, type))
                {
                    nonResource = loaded;
                }

                if (nonResource is T) LoadedData[assetName] = nonResource;
            }

            return nonResource is T t ? t : default;
        }

        /// <summary>
        /// Returns bytes of given asset of given name
        /// </summary>
        public byte[] GetBytes(string assetName, out string ending)
        {
            if (assetName.NotEmpty() && GlobalAddresses.TryGetValue(assetName, out var path) && OE.TryGetData(path.Value, out byte[] buffer) && buffer.NotEmpty())
            {
                ending = path.Value[path.Value.TrimEndUntil('.').Length..];
                return buffer;
            }

            ending = default;
            return default;
        }

        #endregion

        #region QOL Features

        /// <summary>
        /// Instantiates a node from given asset
        /// </summary>
        public N Instantiate<N>(string asset, Godot.Node parent) where N : Godot.Node
        {
            var packed = Get<Godot.PackedScene>(asset);

            if (packed.IsNull())
            {
                Debug.LogError($"Couldn't find asset '{asset}' of typeof({typeof(N).Name})");
                return null;
            }

            return packed.Instantiate<N>(parent);
        }

        public bool TryGetCollection<T>(string dir, out T[] collection) where T : class => (collection = GetCollection<T>(dir)).NotEmpty();

        public T[] GetCollection<T>(string dir) where T : class
        {
            if (IdDirectories.TryGetValue(dir, out var set) == false || set == null || set.Count < 1) return Array.Empty<T>();

            var list = new List<T>();

            foreach (var assetName in set)
            {
                if (TryGet(assetName, out T t))
                    list.Add(t);
            }

            return list.ToArray();
        }

        #endregion

        public static bool LoadDllPck(string dllPath, string pckPath, bool patch = true)
        {
            if (dllPath.NotEmpty() && LoadDll(dllPath) == false) return false;
            if (pckPath.NotEmpty() && LoadPck(pckPath, patch) == false) return false;
            return true;
        }

        public static bool LoadPck(string path, bool patch = true) => ProjectSettings.LoadResourcePack(path, patch);
        public static bool LoadDll(string path)
        {
            try
            {
                Assembly.LoadFile(path);
                return true;
            }

            catch
            {
                return false;
            }
        }
    }
}