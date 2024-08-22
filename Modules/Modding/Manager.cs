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
        private readonly Dictionary<string, object> LoadedNonResources;
        private readonly Dictionary<string, Resource> LoadedResources;

        public readonly Dictionary<string, HashSet<string>> Directories;
        public readonly Dictionary<string, Mod> LoadedMods;
        public readonly Dictionary<string, string> Addresses;

        public readonly Dictionary<string, object> CompilePipeline;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an empty mod
        /// </summary>
        public Manager()
        {
            LoadedNonResources = new();
            LoadedResources = new();
            Directories = new();
            LoadedMods = new();
            Addresses = new();

            CompilePipeline = new();
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

        #region Godot.Resource
        /// <summary>
        /// Returns resource of given resource type. Checks for null references.
        /// </summary>
        public T GetResource<T>(string assetName) where T : Resource
        {
            if (assetName.IsEmpty()) return default;

            if (LoadedResources.TryGetValue(assetName, out var resource) == false || resource.IsNull())
            {
                if (Addresses.TryGetValue(assetName, out var path) && OE.TryGetData<T>(path, out var loaded, IO.FileType.GDResource))
                {
                    resource = loaded;
                }

                if (resource.NotNull() && resource is T) LoadedResources[assetName] = resource;
            }

            return resource is T t ? t : default;
        }

        /// <summary>
        /// Returns array of given resource type. Checks for null references.
        /// </summary>
        public T[] GetResources<T>(string directory) where T : Resource
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

        #region Non Godot.Resource
        /// <summary>
        /// Returns non-resource of given non-resource type. Checks for null references.
        /// </summary>
        public T GetNonResource<T>(string assetName, IO.FileType type = IO.FileType.Binary)
        {
            if (assetName.IsEmpty()) return default;

            if (LoadedNonResources.TryGetValue(assetName, out var nonResource) == false || nonResource == null)
            {
                if (Addresses.TryGetValue(assetName, out var path) && OE.TryGetData<T>(path, out var loaded, type))
                {
                    nonResource = loaded;
                }

                if (nonResource is T) LoadedNonResources[assetName] = nonResource;
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

        #region Write Data
        /// <summary>
        /// Compiles and writes a mod file to given file path. Also adjusts index if wished.
        /// </summary>
        public void Compile(string filePath, Mod meta, bool adjustIndex = true)
        {
            if (adjustIndex)
            {
                var index = new List<string>();

                if (meta.Index.NotEmpty())
                {
                    for (int i = 0; i < meta.Index.Length; i++)
                    {
                        var line = meta.Index[i];
                        if (line.IsEmpty()) continue;

                        var split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (split.Size() < 1) continue;

                        var name = split[0];
                        if (name.IsEmpty()) continue;

                        var path = split.Length > 1 ? split[1] : default;

                        if (path.IsEmpty()) Addresses.TryGetValue(name, out path);

                        if (path.NotEmpty()) index.Add($"{name} {path}");
                    }
                }

                meta.Index = index.ToArray();
            }

            if (meta.Index.IsEmpty())
            {
                Debug.LogError($"Cannot create an empty mod file. Add some assets and add them to the index of your meta file.");
                return;
            }

            var writer = new ZipPacker();
            writer.Open(filePath);

            // Write indexed files
            if (meta.Index.NotEmpty())
            {
                var addedPaths = new HashSet<string>();

                for (int i = 0; i < meta.Index.Length; i++)
                {
                    var line = meta.Index[i];
                    if (adjustIndex == false && line.IsEmpty()) continue;

                    var split = line.Split(' ', Core.StringSplit);
                    if (adjustIndex == false && split.Size() != 2) continue;

                    var name = split[0];
                    var path = split[1];
                    byte[] bytes;

                    // Prevent duplicates
                    if (addedPaths.Contains(path)) continue;
                    addedPaths.Add(path);

                    if (CompilePipeline.TryGetValue(name, out var obj) && obj != null)
                    {
                        if (obj is Resource r)
                        {
                            bytes = FileAccess.GetFileAsBytes(r.ResourcePath);
                        }

                        else
                        {
                            bytes = obj.Encode();
                        }
                    }

                    else bytes = GetBytes(name, out _);

                    if (bytes.IsEmpty()) continue;

                    writer.Append(path, bytes);
                }
            }

            // Write meta file
            writer.Append($"generated{Mod.FILE_ENDING}", meta.GetBuffer());

            writer.Close();
        }
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
                    LoadFile(filePath);
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
        public void LoadFile(string filePath)
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
            if (names.IsEmpty()) LoadedResources.Clear();

            else
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (LoadedResources.ContainsKey(names[i]))
                        LoadedResources.Remove(names[i]);
                }
            }
        }
        #endregion
    }
}