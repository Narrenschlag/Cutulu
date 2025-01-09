namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using System;

    using Godot;
    using Core;

    public static class Parser
    {
        public static (string Name, string Path)[] ParseManifestFiles(string[] filePaths, string rootDirectory)
        {
            var entries = new List<(string Name, string Path)>();

            if (filePaths.NotEmpty())
            {
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var path = FormatPath(filePaths[i], rootDirectory);

                    if (path.NotEmpty() && IO.Exists(path))
                    {
                        var manifest = IO.ReadString(path);

                        if (manifest.NotEmpty()) entries.AddRange(ParseManifest(manifest, path));
                    }

                    else CoreBridge.LogError($"Manifest at path '{path}' does not exist. (root: {rootDirectory})");
                }
            }

            return entries.ToArray();
        }

        public static (string Name, string Path)[] ParseManifest(string text, string rootDirectory)
        {
            if (text.NotEmpty())
            {
                var lines = text.Split('\n');

                if (lines.NotEmpty())
                {
                    var entries = new List<(string, string)>();

                    for (var i = 0; i < lines.Length; i++)
                    {
                        var args = lines[i].Split(' ', Constant.StringSplit);

                        if (args.Size() >= 2)
                        {
                            var pathArgs = args[1].Split(' ', Constant.StringSplit);

                            if (pathArgs.NotEmpty())
                                entries.Add((args[0].RemoveChar(IMod.Seperator), FormatPath(pathArgs[0], rootDirectory)));
                        }
                    }

                    if (entries.Count > 0) return entries.ToArray();
                }
            }

            return Array.Empty<(string, string)>();
        }

        public static string ParseManifest(Dictionary<string, string> dictionary)
        {
            if (dictionary.NotEmpty())
            {
                var stringBuilder = new System.Text.StringBuilder();

                foreach (var entry in dictionary)
                {
                    stringBuilder.AppendLine($"{entry.Key}{IMod.Seperator}{entry.Value}");
                }

                return stringBuilder.ToString();
            }

            return string.Empty;
        }

        public static string ParseManifest((string Key, string Value)[] array)
        {
            if (array.NotEmpty())
            {
                var stringBuilder = new System.Text.StringBuilder();

                foreach (var entry in array)
                {
                    stringBuilder.AppendLine($"{entry.Key}{IMod.Seperator} {entry.Value}");
                }

                return stringBuilder.ToString();
            }

            return string.Empty;
        }

        public static string FormatPath(string filePath, string rootDirectory)
        {
            if ((filePath = filePath.Trim()).StartsWith("res://")) return filePath;

            if (filePath.StartsWith('.'))
            {
                var parentDirectory = rootDirectory.TrimToDirectory();
                byte layers = 0;

                for (var i = 0; i < filePath.Length; i++)
                {
                    if (filePath[i] == '.') layers++;
                    else break;
                }

                filePath = filePath[(layers + 1)..];

                if (parentDirectory.NotEmpty())
                {
                    var args = parentDirectory.Trim().Split(new[] { '/', '\\' }, Constant.StringSplit);

                    if (args.Size() > layers)
                    {
                        if (args[0] == "res:") args[0] = "res:/";
                        parentDirectory = "";

                        for (var i = 0; i < args.Length - layers + 1; i++)
                            parentDirectory += $"{args[i]}/";

                        return $"{parentDirectory}{filePath}";
                    }
                }
            }

            return $"res://{filePath}";
        }

        public static string MakePathRelative(string filePath, string rootDirectory)
        {
            rootDirectory = ProjectSettings.GlobalizePath(rootDirectory.TrimToDirectory());
            var fileDirectory = ProjectSettings.GlobalizePath(filePath.TrimToDirectory());

            var fileName = ProjectSettings.GlobalizePath(filePath)[fileDirectory.Length..];

            if (rootDirectory == fileDirectory) return $"./{fileName}";

            if (fileDirectory.StartsWith(rootDirectory)) return $"./{fileDirectory[rootDirectory.Length..]}{fileName}";

            var argsFile = fileDirectory.Split(new[] { '/', '\\' }, Constant.StringSplit);
            var argsRoot = rootDirectory.Split(new[] { '/', '\\' }, Constant.StringSplit);
            int steps = 1, offset = 0;

            for (int i = argsRoot.Length - 1; i >= 0; i--)
            {
                var found = false;

                for (int k = argsFile.Length - 1; k >= 0; k--)
                {
                    // Found same
                    if (argsFile[k] == argsRoot[i])
                    {
                        found = true;
                        offset = k;
                        break;
                    }
                }

                if (found) break;
                else steps++;
            }

            return $"{"".Fill(steps, '.')}/{argsFile[(offset + 1)..].Join("/")}{(offset < argsFile.Length - 1 ? '/' : "")}{fileName}";
        }
    }
}