using System.Collections.Generic;
using System.Text;
using System;
using Godot;

namespace Cutulu
{
    public class WAD
    {
        private Dictionary<string, object> LoadedNonResources;
        private Dictionary<string, Resource> LoadedResources;

        public Dictionary<string, HashSet<string>> Directories { get; private set; }
        public Dictionary<string, string> Addresses { get; private set; }

        public WAD(params string[] directories)
        {
            LoadedNonResources = new();
            LoadedResources = new();
            Directories = new();
            Addresses = new();
            var packCount = 0;

            foreach (var directory in directories)
            {
                if (DirAccess.DirExistsAbsolute(directory) == false) continue;

                var files = DirAccess.GetFilesAt(directory);
                if (files.IsEmpty()) continue;

                var reader = new ZipReader();
                foreach (var file in files)
                {
                    try
                    {
                        var err = reader.Open($"{directory}{file}");
                        if (err != Error.Ok) throw new("File is not a zip archive.");

                        if (reader.FileExists("index.meta") == false) throw new("No index.meta file could be found.");

                        if (reader.ReadFile("index.meta").TryBuffer(out WADMeta meta) == false) continue;
                        Debug.Log($"Loading WAD Pack '{meta.Name}'({meta.Index.Size()} files) by '{meta.Author}'. ({meta.Description})");

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
                                Addresses[name] = $"{directory}{file}?{path}";

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

                        packCount++;
                    }

                    catch (Exception ex)
                    {
                        Debug.LogError($"Cannot load assets of {directory}{file}\n{ex.Message}\n{ex.StackTrace}");
                    }
                }

                reader.Close();
            }

            Debug.Log($"Loaded {Addresses.Count} assets from {packCount} asset packs");
        }

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

                if (resource.NotNull()) LoadedResources[assetName] = resource;
            }

            return resource is T t ? t : default;
        }

        public T GetNonResource<T>(string assetName)
        {
            if (LoadedNonResources.TryGetValue(assetName, out var nonResource) == false || nonResource == null)
            {
                var bytes = GetBytes(assetName, out _);

                if (bytes.NotEmpty())
                {
                    // Load resource from temp file
                    nonResource = bytes.Buffer<T>();
                }

                if (nonResource == null) LoadedNonResources[assetName] = nonResource;
            }

            return nonResource is T t ? t : default;
        }

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
    }
}