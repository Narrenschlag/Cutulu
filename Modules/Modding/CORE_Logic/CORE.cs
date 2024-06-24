using System.Collections.Generic;
using System.Text;
using System;
using Godot;

namespace Cutulu.Modding
{
    public class CORE
    {
        #region Params
        private readonly Dictionary<string, object> LoadedNonResources;
        private readonly Dictionary<string, Resource> LoadedResources;

        public readonly Dictionary<string, HashSet<string>> Directories;
        public readonly Dictionary<string, string> Addresses;
        #endregion

        #region Constructors
        public CORE()
        {
            LoadedNonResources = new();
            LoadedResources = new();
            Directories = new();
            Addresses = new();
        }

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
        public void Compile(string filePath, COREMeta meta, bool adjustIndex = true)
        {
            var writer = new ZipPacker();
            writer.Open(filePath);

            if (adjustIndex)
            {
                var index = new List<string>();
                meta.Index = index.ToArray();

                if (meta.Index.NotEmpty())
                {
                    for (int i = 0; i < meta.Index.Length; i++)
                    {
                        var line = meta.Index[i];
                        if (line.IsEmpty()) continue;

                        var split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (split.Size() != 2) continue;

                        var name = split[0];

                        if (Addresses.TryGetValue(name, out var path))
                        {
                            index.Add($"{name} {path}");
                        }
                    }
                }

                meta.Index = index.ToArray();
            }

            // Write meta file
            writer.AddEntry("index.meta", meta.Buffer());

            // Write indexed files
            if (meta.Index.NotEmpty())
            {
                for (int i = 0; i < meta.Index.Length; i++)
                {
                    var line = meta.Index[i];
                    if (adjustIndex == false && line.IsEmpty()) continue;

                    var split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (adjustIndex == false && split.Size() != 2) continue;

                    var name = split[0];
                    var path = split[1];

                    var bytes = GetBytes(name, out _);
                    if (bytes.IsEmpty()) continue;

                    writer.AddEntry(path, bytes);
                }
            }

            writer.Close();
        }
        #endregion

        #region (Un)loading
        public void Load(string filePath) => Load(new[] { filePath });
        public void Load(params string[] filePaths)
        {
            if (filePaths.IsEmpty()) return;

            var reader = new ZipReader();

            Load(ref reader, filePaths);

            reader.Close();
        }

        public void Load(ref ZipReader reader, string filePath) => Load(ref reader, new[] { filePath });
        private void Load(ref ZipReader reader, params string[] filePaths)
        {
            if (filePaths.IsEmpty()) return;

            foreach (var filePath in filePaths)
            {
                try
                {
                    var err = reader.Open(filePath);
                    if (err != Error.Ok) throw new("File is not a zip archive.");

                    if (COREMeta.TryRead(ref reader, filePath, out var meta) == false) throw new("No index.meta file could be found.");

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
                    Debug.LogError($"Cannot load assets of {filePath}\n{ex.Message}\n{ex.StackTrace}");
                }
            }
        }

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