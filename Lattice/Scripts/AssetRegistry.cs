namespace Cutulu.Lattice
{
    using System.Collections.Generic;

    public static class AssetRegistry
    {
        private static readonly Dictionary<string, string> AssetPaths = new();

        // Register an asset by ID and file path
        public static void RegisterAsset(string id, string filePath)
        {
            if (AssetPaths.ContainsKey(id))
            {
                CoreBridge.LogError($"Duplicate asset ID detected: {id}. Skipping registration.");
                return;
            }

            AssetPaths[id] = filePath;
        }

        // Get the file path of an asset by ID
        public static string GetAssetPath(string id)
        {
            return AssetPaths.TryGetValue(id, out var path) ? path : null;
        }

        // Check if an asset is registered
        public static bool HasAsset(string id)
        {
            return AssetPaths.ContainsKey(id);
        }
    }
}