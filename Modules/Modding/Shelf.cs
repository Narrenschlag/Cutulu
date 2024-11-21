namespace Cutulu.Modding
{
    using System.Collections.Generic;

    public partial class Shelf
    {
        public readonly Dictionary<string, string[]> Addresses = new();
        public readonly string FilePath;
        public readonly Library Parent;
        public readonly Data Data;

        public bool Enabled { get; private set; }
        public int Priority { get; set; }

        public Shelf(Library parent, string filePath)
        {
            // Try find mod data from given file path
            if (OE.TryGetData(FilePath = filePath, out Data, Constants.FILE_TYPE) == false) throw new System.IO.FileNotFoundException($"File not found.");

            // Assign parent
            Parent = parent;

            // Assign default priority
            Priority = Data.DefaultPriority;

            // Check if there are any local addresses
            if (Data.LocalAddresses.NotEmpty())
            {
                // Trim file path into directory
                var directory = filePath.TrimToDirectory('/', '\\', '?');

                // Iterate through addresses
                foreach (var address in Data.LocalAddresses)
                {
                    // Split string into key and value strings
                    var arr = address.Split(Constants.ADDRESS_SEPERATOR, Cutulu.Constants.StringSplit);
                    if (arr.Size() != 2) continue;

                    // Validate path
                    var path = $"{directory}{arr[1]}";
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
            Enabled = false;

            if (refresh) Parent?.Refresh();
        }
    }
}