using System.Collections.Generic;
using System.Text;
using System;
using Godot;

namespace Cutulu.Modding
{
    /// <summary>
    /// CORE - Collection of Realms and Entities<br/>
    /// Allows your players and you to patch your project using asset collections(mods)
    /// </summary>
    public class CORE
    {
        #region Params
        public const string META_PATH = "CORE.META";

        private readonly Dictionary<string, object> LoadedNonResources;
        private readonly Dictionary<string, Resource> LoadedResources;

        public readonly Dictionary<string, HashSet<string>> Directories;
        public readonly Dictionary<string, string> Addresses;

        public readonly Dictionary<string, object> CompilePipeline;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an empty CORE
        /// </summary>
        public CORE()
        {
            LoadedNonResources = new();
            LoadedResources = new();
            Directories = new();
            Addresses = new();

            CompilePipeline = new();
        }

        /// <summary>
        /// Creates a CORE containing given CORE files
        /// </summary>
        public CORE(params string[] directories) : this()
        {
            var reader = new ZipReader();
            var packCount = 0;

            foreach (var directory in directories)
            {
                if (DirAccess.DirExistsAbsolute(directory) == false) continue;

                var files = DirAccess.GetFilesAt(directory);
                if (files.IsEmpty()) continue;

                var paths = new List<string>();
                foreach (var file in files)
                {
                    paths.Add($"{directory}{file}");
                }

                Load(ref reader, paths.ToArray());
            }

            Debug.Log($"Loaded {Addresses.Count} assets from {packCount} asset packs");
            reader.Close();
        }
        #endregion

        #region Read Data

        #region Godot.Resource
        /// <summary>
        /// Returns resource of given non-resource type. Checks for null references.
        /// </summary>
        public T GetResource<T>(string assetName) where T : Resource
        {
            if (LoadedResources.TryGetValue(assetName, out var resource) == false || resource.IsNull())
            {
                var bytes = GetBytes(assetName, out var ending);

                if (bytes.NotEmpty())
                {
                    var type = typeof(T);

                    // Support for Models
                    if (type == typeof(GlbModel)) resource = GlbModel.CustomImport(bytes);

                    // Support for OGG files
                    else if (type == typeof(AudioStream)) resource = AudioStreamOggVorbis.LoadFromBuffer(bytes);

                    else
                    {
                        // Mkdir the file path
                        var temp = $"{IO.USER_PATH}.bin/.temp/";
                        DirAccess.MakeDirRecursiveAbsolute(temp);

                        // Write a temp file to read the resource from
                        (temp = $"{temp}temp.{ending}").WriteBytes(bytes);

                        try
                        {
                            // Support for Texture2D
                            if (type == typeof(Texture2D))
                            {
                                var img = new Image();
                                img.Load(temp);

                                resource = ImageTexture.CreateFromImage(img) as Texture2D;
                            }

                            // Load resource from temp file
                            else resource = GD.Load<T>(temp);
                        }

                        catch (Exception ex)
                        {
                            Debug.LogError($"Cannot load {typeof(T).Name}\n{ex.Message}");
                        }

                        temp.DeleteFile();
                    }
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
            if (LoadedNonResources.TryGetValue(assetName, out var nonResource) == false || nonResource == null)
            {
                var bytes = GetBytes(assetName, out _);

                if (bytes.NotEmpty())
                {
                    // Load resource from temp file
                    switch (type)
                    {
                        case IO.FileType.Json:
                            nonResource = Encoding.UTF8.GetString(bytes).json<T>();
                            break;

                        default:
                            nonResource = bytes.Buffer<T>();
                            break;
                    }
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
            byte[] bytes = null;
            ending = null;

            if (Addresses.TryGetValue(assetName, out var _path))
            {
                var path = _path.Split('?', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (path.Size() == 2 && FileAccess.FileExists(path[0]))
                {
                    using var reader = new ZipReader();

                    if (reader.Open(path[0]) != Error.Ok) return bytes;
                    var filePath = path[1];

                    if (reader.FileExists(filePath))
                    {
                        var splitEnding = path[1].Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        ending = splitEnding.NotEmpty() ? splitEnding[^1] : null;

                        bytes = reader.ReadFile(filePath);
                    }

                    reader.Close();
                }
            }

            return bytes;
        }
        #endregion

        #endregion

        #region Write Data
        /// <summary>
        /// Compiles and writes a CORE file to given file path. Also adjusts index if wished.
        /// </summary>
        public void Compile(string filePath, COREMeta meta, bool adjustIndex = true)
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
                Debug.LogError($"Cannot create an empty CORE file. Add some assets and add them to the index of your meta file.");
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

                    var split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (adjustIndex == false && split.Size() != 2) continue;

                    var name = split[0];
                    var path = split[1];

                    byte[] bytes = null;

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
                            bytes = obj.Buffer();
                        }
                    }

                    else bytes = GetBytes(name, out _);

                    if (bytes.IsEmpty()) continue;

                    writer.Append(path, bytes);
                }
            }

            // Write meta file
            writer.Append(META_PATH, meta.Buffer());
            var buffer = meta.Buffer();

            writer.Close();
        }
        #endregion

        #region (Un)loading
        /// <summary>
        /// Loads in a core file from given path, if possible.
        /// </summary>
        public void Load(string filePath) => Load(new[] { filePath });

        /// <summary>
        /// Loads in a core file from given paths, if possible.
        /// </summary>
        public void Load(params string[] filePaths)
        {
            if (filePaths.IsEmpty()) return;

            var reader = new ZipReader();

            Load(ref reader, filePaths);

            reader.Close();
        }

        private void Load(ref ZipReader reader, params string[] filePaths)
        {
            if (filePaths.IsEmpty()) return;

            foreach (var filePath in filePaths)
            {
                try
                {
                    var err = reader.Open(filePath);
                    if (err != Error.Ok) throw new("File is not a zip archive.");

                    if (COREMeta.TryRead(ref reader, filePath, out var meta) == false) throw new($"No {META_PATH} file could be found.");

                    Debug.Log($"Loading CORE '{meta.Name}'({meta.Index.Size()} files) by '{meta.Author}'. ({meta.Description})");

                    var strng = new StringBuilder();

                    // Seperated by lines
                    if (meta.Index.NotEmpty())
                    {
                        for (int i = 0; i < meta.Index.Length; i++)
                        {
                            // Seperated by spaces
                            var args = meta.Index[i].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (args.Size() != 2) continue;

                            var path = args[1];

                            if (reader.FileExists(path) == false) continue;

                            var name = args[0];

                            // Global address for the name
                            Addresses[name] = $"{filePath}?{path}";

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

                catch (Exception ex)
                {
                    Debug.LogError($"Cannot load assets of {filePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unloads either all COREs or given assets/names
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