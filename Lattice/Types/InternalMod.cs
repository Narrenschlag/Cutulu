namespace Cutulu.Lattice
{
    using System;
    using System.Collections.Generic;
    using Cutulu.Core;
    using Godot;

    [GlobalClass]
    public partial class InternalMod : Resource, IMod
    {
        [Export] public string ID { get; set; } = "internal_id";
        [Export] public string Name { get; set; } = "InternalMod";
        [Export] public string Author { get; set; } = "Narrenschlag";
        [Export] public string Version { get; set; } = "1.0.0";
        [Export] public string Description { get; set; } = "A basic example mod.";
        [Export] public string[] Dependencies { get; set; }

        [Export(PropertyHint.MultilineText)]
        public string AssetManifest =
            $"name{IMod.Seperator} res://resource_path" +
            $"\nname{IMod.Seperator} resource_path            // Equivalent to res://resource_path" +
            $"\nname{IMod.Seperator} ./resource_path          // Relative to this resource" +
            $"\nname{IMod.Seperator} ../resource_path         // Relative to the parent folder of this resource" +
            $"\nname{IMod.Seperator} .../resource_path        // Relative to the parent folder of the parent folder of this resource";

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

        public string GetPath(string filePath) => Parser.FormatPath(filePath, ResourcePath);

        public (string Name, string Path)[] ReadAssetEntries() => Parser.ParseManifest(AssetManifest, ResourcePath);
        public (string Name, string Path)[] ReadPackageEntries() => Array.Empty<(string, string)>();
        public (string Name, string Path)[] ReadAssemblyEntries() => Array.Empty<(string, string)>();
    }
}