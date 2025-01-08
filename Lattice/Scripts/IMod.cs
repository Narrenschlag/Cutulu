namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using Cutulu.Core;

    public interface IMod
    {
        public const string FileEnding = ".duck";
        public const char Seperator = ':';

        public string ID { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string[] Dependencies { get; set; }

        public abstract void Load();
        public abstract void Unload();

        public abstract void Activate();
        public abstract void Deactivate();

        public abstract string GetPath(string filePath);
        public abstract (string Name, string Path)[] ReadAssetEntries();
        public abstract (string Name, string Path)[] ReadPackageEntries();
        public abstract (string Name, string Path)[] ReadAssemblyEntries();
    }
}