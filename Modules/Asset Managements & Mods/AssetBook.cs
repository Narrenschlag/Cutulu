namespace Cutulu
{
    using System.Collections.Generic;

    public partial class AssetBook
    {
        public readonly Dictionary<string, string[]> Addresses = new();
        public readonly string FilePath, RootDirectory;
        public readonly AssetLibrary Parent;
        public readonly AssetBookData Data;

        public bool Enabled { get; private set; }
        public int Priority { get; set; }

        public AssetBook(AssetLibrary parent, string filePath)
        {
            // Try find mod data from given file path
            if (OE.TryGetData(FilePath = filePath, out Data, AssetConstants.FILE_TYPE) == false) throw new System.IO.FileNotFoundException($"File not found.");

            RootDirectory = filePath.TrimToDirectory('/', '\\', '?');

            // Assign parent
            Parent = parent;

            // Assign default priority
            Priority = Data.DefaultPriority;

            // Check if there are any local addresses
            if (Data.AliasIndex.NotEmpty())
            {
                // Iterate through addresses
                foreach (var address in Data.AliasIndex)
                {
                    // Split string into key and value strings
                    var arr = address.Split(AssetConstants.ADDRESS_SEPERATOR, Constants.StringSplit);
                    if (arr.Size() != 2) continue;

                    // Validate path
                    var path = $"{IO.PROJECT_PATH}{arr[1]}";
                    if (OE.Exists(path) == false) continue;

                    // Assign paths
                    Addresses[arr[0]] = new[] { path, arr[1] };
                }
            }
        }

        /// <summary>
        /// Enables mod
        /// <summar>
        public void Enable(bool refresh = true)
        {
            // Check for dependencies
            if (Data.Dependencies.NotEmpty())
            {
                foreach (var dependency in Data.Dependencies)
                {
                    if (Parent.LoadedMods.ContainsKey(dependency) == false)
                    {
                        Debug.LogError($"Dependency '{dependency}' for '{Data.Id}' is not present. Unable to enable.");

                        if (Enabled) Disable(refresh);
                        return;
                    }
                }
            }

            Enabled = true;

            if (refresh) Parent?.Refresh();
        }

        /// <summary>
        /// Disables mod
        /// <summar>
        public void Disable(bool refresh = true)
        {
            if (Enabled && Data.ForceEnabled) return;

            Enabled = false;

            if (refresh) Parent?.Refresh();
        }
    }
}