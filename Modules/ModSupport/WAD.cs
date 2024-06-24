using System.Collections.Generic;
using System.Text;
using System;
using Godot;

namespace Cutulu
{
    public class WAD
    {
        private Dictionary<string, object> LoadedNonResources;
        private Dictionary<string, Resource> LoadedResource;
        private Dictionary<string, string> Addresses;

        public WAD(params string[] directories)
        {
            LoadedNonResources = new();
            LoadedResource = new();
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

                        var index = Encoding.UTF8.GetString(reader.ReadFile("index.meta"));

                        // Seperated by lines
                        var lines = index.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (lines.NotEmpty())
                        {
                            for (int k = 0; k < lines.Length; k++)
                            {
                                // Seperated by spaces
                                var args = lines[k].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                if (args.Size() != 2) continue;

                                var path = args[1];

                                if (reader.FileExists(path) == false) continue;

                                var name = args[0];

                                Addresses[name] = $"{directory}{file}?{path}";
                            }
                        }

                        packCount++;
                    }

                    catch (Exception ex)
                    {
                        Debug.LogError($"Cannot load assets of {directory}{file}.\n{ex.Message}");
                    }
                }

                reader.Close();
            }

            Debug.Log($"Loaded {Addresses.Count} assets from {packCount} asset packs");
        }

        public T GetResource<T>(string assetName) where T : Resource
        {
            if (LoadedResource.TryGetValue(assetName, out var resource) == false || resource.IsNull())
            {
                var bytes = GetBytes(assetName, out var ending);

                if (bytes.NotEmpty())
                {
                    var type = typeof(T);
                    if (type == typeof(GlbModel)) resource = GlbModel.CustomImport(bytes);

                    else
                    {
                        // Mkdir the file path
                        var temp = $"{IO.USER_PATH}.bin/.temp/";
                        DirAccess.MakeDirRecursiveAbsolute(temp);

                        // Write a temp file to read the resource from
                        (temp = $"{temp}temp.{ending}").WriteBytes(bytes);

                        // Load resource from temp file
                        resource = GD.Load<T>(temp);
                        temp.DeleteFile();
                    }
                }

                if (resource.NotNull()) LoadedResource[assetName] = resource;
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

        public void Unload(params string[] names)
        {
            if (names.IsEmpty()) LoadedResource.Clear();

            else
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (LoadedResource.ContainsKey(names[i]))
                        LoadedResource.Remove(names[i]);
                }
            }
        }
    }
}