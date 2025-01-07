namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using Cutulu.Core;
    using System;

    public interface IMod
    {
        public const char Seperator = ':';

        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string[] Dependencies { get; set; }

        public abstract void Initialize();
        public abstract void Activate();
        public abstract void Deactivate();
        public abstract void Unload();

        public abstract string GetPath(string filePath);
        public abstract (string Name, string Path)[] ReadAssetEntries();
        public abstract (string Name, string Path)[] ReadPackageEntries();
        public abstract (string Name, string Path)[] ReadAssemblyEntries();

        public static string WriteManifest(Dictionary<string, string> dictionary)
        {
            if (dictionary.NotEmpty())
            {
                var stringBuilder = new System.Text.StringBuilder();

                foreach (var entry in dictionary)
                {
                    stringBuilder.AppendLine($"{entry.Key}{Seperator}{entry.Value}");
                }

                return stringBuilder.ToString();
            }

            return string.Empty;
        }
    }
}