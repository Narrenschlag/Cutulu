using System.Collections.Generic;
using System.Text;
using System;
using Godot;

namespace Cutulu
{
    public class WAD
    {
        private Dictionary<string, Resource> LoadedAssets;
        private Dictionary<string, string> PassiveAssets;

        public WAD(params string[] directories)
        {
            PassiveAssets = new();
            LoadedAssets = new();
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

                                PassiveAssets[name] = $"{directory}{file}?{path}";
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

            Debug.Log($"Loaded {PassiveAssets.Count} assets from {packCount} asset packs");
        }

        public T GetResource<T>(string assetName) where T : Resource
        {
            if (LoadedAssets.TryGetValue(assetName, out var resource) == false || resource.IsNull())
            {
                if (PassiveAssets.TryGetValue(assetName, out var _path))
                {
                    var path = _path.Split('?', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (path.Size() == 2 && FileAccess.FileExists(path[0]))
                    {
                        using var reader = new ZipReader();
                        reader.Open(path[0]);

                        var filePath = path[1];

                        if (reader.FileExists(filePath))
                        {
                            // Mkdir the file path
                            var temp = $"{IO.USER_PATH}.bin/.temp/";
                            DirAccess.MakeDirRecursiveAbsolute(temp);

                            // Write a temp file to read the resource from
                            var bytes = reader.ReadFile(filePath);
                            (temp = $"{temp}temp.tres").WriteBytes(bytes);

                            // Load resource from temp file
                            resource = GD.Load<T>(temp);
                            temp.DeleteFile();
                        }

                        reader.Close();
                    }
                }

                if (resource.NotNull()) LoadedAssets[assetName] = resource;
            }

            return resource is T t ? t : default;
        }

        public void Unload(params string[] names)
        {
            if (names.IsEmpty()) LoadedAssets.Clear();

            else
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (LoadedAssets.ContainsKey(names[i]))
                        LoadedAssets.Remove(names[i]);
                }
            }
        }
    }
}