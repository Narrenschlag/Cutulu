namespace Cutulu.Lattice
{
    using Cutulu.Core;

    public partial class ExternalMod : IMod
    {
        public string ID { get; set; } = "external_id";
        public string Name { get; set; } = "ExternalMod";
        public string Author { get; set; } = "Narrenschlag";
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = "A basic example mod.";
        public string[] Dependencies { get; set; }

        public string[] AssetManifestPaths { get; set; } = new[] { "./assets.manifest" };
        public string[] AssemblyManifestPaths { get; set; } = new[] { "./assemblies.manifest" };
        public string[] GodotPackageManifestPaths { get; set; } = new[] { "./packages.manifest" };

        [DontEncode] public string FilePath { get; set; }

        public virtual void Load()
        {
            CoreBridge.Log($"{Name} initialized!");
        }

        public virtual void Activate()
        {
            CoreBridge.Log($"{Name} activated!");
        }

        public virtual void Deactivate()
        {
            CoreBridge.Log($"{Name} deactivated!");
        }

        public virtual void Unload()
        {
            CoreBridge.Log($"{Name} unloaded!");
        }

        public string GetPath(string filePath) => Parser.FormatPath(filePath, FilePath);

        public (string Name, string Path)[] ReadAssetEntries() => Parser.ParseManifestFiles(AssetManifestPaths, FilePath);
        public (string Name, string Path)[] ReadAssemblyEntries() => Parser.ParseManifestFiles(AssemblyManifestPaths, FilePath);
        public (string Name, string Path)[] ReadPackageEntries() => Parser.ParseManifestFiles(GodotPackageManifestPaths, FilePath);
    }
}