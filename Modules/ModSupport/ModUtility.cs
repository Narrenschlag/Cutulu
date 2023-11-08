namespace Cutulu
{
    public static class ModUtility
    {
        // Constant deault values
        private const string DefaultAssetsFolder = "assets/";
        private const string DefaultModFolder = "mods/";

        /// <summary>Load asset from file system, decide if you prefer the mod version</summary>
        public static bool TryLoad<T>(this string local, out T asset, bool preferMod, string assetFolder = DefaultAssetsFolder, string modFolder = DefaultModFolder) where T : class
            => preferMod ?
                TryLoadMod(local, out asset, modFolder) ? true : TryLoadAsset(local, out asset, assetFolder) :  // Check mod folder then asset folder
                TryLoadAsset(local, out asset, assetFolder) ? true : TryLoadMod(local, out asset, modFolder);   // Check asset folder then mod folder

        /// <summary>Load asset from file system, preferes the mod folder<summary>
        public static bool TryLoadM<T>(this string local, out T asset, string assetFolder = DefaultAssetsFolder, string modFolder = DefaultModFolder) where T : class
            => TryLoad(local, out asset, true, assetFolder, modFolder);

        /// <summary>Load asset from mod folder<summary>
        public static bool TryLoadMod<T>(this string local, out T asset, string modFolder = DefaultModFolder) where T : class
            => IO.TryLoad($"{IO.USER_PATH}{modFolder}{local}", out asset);

        /// <summary>Load asset from asset folder<summary>
        public static bool TryLoadAsset<T>(this string local, out T asset, string assetFolder = DefaultAssetsFolder) where T : class
            => IO.TryLoad($"{IO.USER_PATH}{assetFolder}{local}", out asset);
    }
}