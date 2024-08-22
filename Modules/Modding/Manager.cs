using System.Collections.Generic;
using System.Text;
using System;
using Godot;

namespace Cutulu.Modding
{
    /// <summary>
    /// Allows your players and you to patch your project using asset collections(mods)
    /// </summary>
    public class Manager
    {
        #region Params
        public readonly Dictionary<string, HashSet<string>> Directories = new();
        public readonly Dictionary<string, string> Addresses = new();

        private readonly Dictionary<string, object> Loaded = new();
        public readonly Dictionary<string, Mod> LoadedMods = new();
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an empty mod
        /// </summary>
        public Manager()
        {

        }

        /// <summary>
        /// Creates a mod containing given mod files
        /// </summary>
        public Manager(params string[] rootDirectories) : this()
        {
            Load(rootDirectories);

            Debug.Log($"Loaded {Addresses.Count} assets from {LoadedMods.Count} asset packs");
        }
        #endregion

        #region Read Data

        public T Get<T>(string identifier)
        {
            return TryGet(identifier, out T t) ? t : default;
        }

        public bool TryGet<T>(string identifier, out T output)
        {
            if (typeof(T).IsSubclassOf(typeof(Resource)))
                output = GetResource<T>(identifier);

            else
                output = GetNonResource<T>(identifier);

            return output is not null;
        }

        #region Non Godot.Resource
        /// <summary>
        /// Returns non-resource of given non-resource type. Checks for null references.
        /// </summary>
        public T GetNonResource<T>(string assetName, IO.FileType type = IO.FileType.Binary)
        {
            if (assetName.IsEmpty()) return default;

            if (Loaded.TryGetValue(assetName, out var nonResource) == false || nonResource == null)
            {
                if (Addresses.TryGetValue(assetName, out var path) && OE.TryGetData<T>(path, out var loaded, type))
                {
                    nonResource = loaded;
                }

                if (nonResource is T) Loaded[assetName] = nonResource;
            }

            return nonResource is T t ? t : default;
        }

        /// <summary>
        /// Returns array of given non-resource type. Checks for null references.
        /// </summary>
        public T[] GetNonResources<T>(string directory, IO.FileType type = IO.FileType.Binary)
        {
            var list = new List<T>();

            if (Directories.TryGetValue(directory, out var set))
            {
                foreach (var assetName in set)
                {
                    var nonResource = GetNonResource<T>(assetName, type);

                    if (nonResource != null) list.Add(nonResource);
                }
            }

            return list.ToArray();
        }
        #endregion

        #region Godot.Resource
        /// <summary>
        /// Returns resource of given resource type. Checks for null references.
        /// </summary>
        private T GetResource<T>(string assetName)
        {
            if (assetName.IsEmpty()) return default;

            if (Loaded.TryGetValue(assetName, out var output) == false || output is not T)
            {
                if (Addresses.TryGetValue(assetName, out var path) && OE.TryGetData<T>(path, out var loaded, IO.FileType.GDResource))
                    output = loaded;

                if (output is T && output is Resource _output) Loaded[assetName] = _output;
            }

            return output is T t ? t : default;
        }

        /// <summary>
        /// Returns array of given resource type. Checks for null references.
        /// </summary>
        private T[] GetResources<T>(string directory) where T : Resource
        {
            var list = new List<T>();

            if (Directories.TryGetValue(directory, out var set))
            {
                foreach (var assetName in set)
                {
                    var resource = GetResource<T>(assetName);

                    if (resource.NotNull()) list.Add(resource);
                }
            }

            return list.ToArray();
        }
        #endregion

        #region Bytes
        /// <summary>
        /// Returns bytes of given asset of given name
        /// </summary>
        public byte[] GetBytes(string assetName, out string ending)
        {
            if (assetName.NotEmpty() && Addresses.TryGetValue(assetName, out var path) && OE.TryGetData(path, out var buffer))
            {
                ending = path[path.TrimEndUntil('.').Length..];
                return buffer;
            }

            ending = default;
            return default;
        }
        #endregion

        #endregion

        #region Loading
        /// <summary>
        /// Loads in mod files from given paths, if possible. Also loads in nested packs in other packs or directories.
        /// </summary>
        public bool Load(params string[] rootDirectories)
        {
            if (rootDirectories.IsEmpty()) return false;

            foreach (var rootDir in rootDirectories)
            {
                var filePaths = new List<string>();

                OE.FindFiles(rootDir, ref filePaths, new[] { Mod.FILE_ENDING }, new[] { ".zip" });

                foreach (var filePath in filePaths)
                {
                    LoadMod(filePath);
                }
            }

            foreach (var meta in LoadedMods.Values)
            {
                if (meta.Dependencies.NotEmpty())
                {
                    foreach (var modId in meta.Dependencies)
                    {
                        if (LoadedMods.ContainsKey(modId) == false)
                        {
                            Debug.LogError($"{meta.Name} is missing dependency mod '{modId}'. Maybe the loading order is wrong.");
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Loads in a mod file from given path, if possible.
        /// </summary>
        public void LoadMod(string filePath)
        {
            // Try read meta file
            if (Mod.TryRead(filePath, out var meta) == false) return;

            var modDir = filePath.TrimToDirectory('/', '\\', '?');

            if (LoadedMods.TryGetValue(meta.ModId, out var overwritten))
                Debug.LogError($"Present mod <{overwritten.Name}>({meta.ModId}) is overwritten by <{meta.Name}> with the same identifier. This could lead to dependency issues.");
            LoadedMods[meta.ModId] = meta;

            if (meta.Index.NotEmpty())
            {
                foreach (var entry in meta.Index)
                {
                    // Seperated by spaces
                    var args = entry.Split(' ', Core.StringSplit);
                    if (args.Size() != 2) continue;

                    var localPath = args[1];
                    var name = args[0];

                    var path = $"{modDir}{localPath}";
                    var strng = new StringBuilder();

                    if (OE.Exists($"{modDir}{localPath}"))
                    {
                        // Global address for the name
                        Addresses[name] = $"{path}";

                        // Add dictionaries with depth for targetting specific types
                        var directorySplits = name.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (directorySplits.Size() > 1)
                        {
                            for (int k = 0; k < directorySplits.Length - 1; k++)
                            {
                                strng.Clear();

                                for (int j = 0; j <= k; j++)
                                {
                                    if (j > 0) strng.Append('/');
                                    strng.Append(directorySplits[j]);
                                }

                                var dir = strng.ToString();
                                if (Directories.TryGetValue(dir, out var set) == false || dir.IsEmpty())
                                {
                                    Directories[dir] = set = new();
                                }

                                set.Add(name);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Unloading
        /// <summary>
        /// Unloads either all mods or given assets/names
        /// </summary>
        public void Unload(params string[] names)
        {
            if (names.IsEmpty()) Loaded.Clear();

            else
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (Loaded.ContainsKey(names[i]))
                        Loaded.Remove(names[i]);
                }
            }
        }
        #endregion
    }
}