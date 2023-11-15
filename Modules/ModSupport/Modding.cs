using System.Collections.Generic;

namespace Cutulu
{
    public static class Modding
    {
        #region File Management (IO)
        // Constant deault values
        private const string DefaultAssetsFolder = "Assets/";
        private const string DefaultModFolder = "Mods/";

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
            => IO.TryLoad($"{IO.PROJECT_PATH}{assetFolder}{local}", out asset);
        #endregion

        #region Json
        public static bool TryLoadJson<T>(this string local, out T asset, bool preferMod = true, string assetFolder = DefaultAssetsFolder, string modFolder = DefaultModFolder)
            => preferMod ?
                TryLoadModJson(local, out asset, modFolder) ? true : TryLoadAssetJson(local, out asset, assetFolder) :  // Check mod folder then asset folder
                TryLoadAssetJson(local, out asset, assetFolder) ? true : TryLoadModJson(local, out asset, modFolder);   // Check asset folder then mod folder

        public static bool TryLoadModJson<T>(this string local, out T asset, string modFolder = DefaultModFolder)
            => IO.TryLoadJson(local.LocalToGlobal(IO.USER_PATH + modFolder), out asset);

        public static bool TryLoadAssetJson<T>(this string local, out T asset, string assetFolder = DefaultAssetsFolder)
            => IO.TryLoadJson(local.LocalToGlobal(IO.PROJECT_PATH + assetFolder), out asset);

        public static string LocalToGlobal(this string local, bool mod = true, string assetFolder = DefaultAssetsFolder, string modFolder = DefaultModFolder)
            => $"{(mod ? IO.USER_PATH : IO.PROJECT_PATH)}{(mod ? modFolder : assetFolder)}{local}";

        public static string LocalToGlobal(this string local, string folder)
            => $"{folder}{local}";
        #endregion

        #region Caching
        private static Dictionary<string, Dictionary<string, object>> Cache;

        /// <summary>Load preferably modded file from local path at folder</summary>
        public static bool TryLoadCached<T>(this string localPath, string folder, out T value, bool preferMod = true, string assetFolder = DefaultAssetsFolder, string modFolder = DefaultModFolder) where T : class
        {
            folder = folder.Trim();
            localPath.Trim();

            if (Cache == null) Cache = new Dictionary<string, Dictionary<string, object>>();
            if (!Cache.TryGetValue(folder, out Dictionary<string, object> _cache))
            {
                _cache = new Dictionary<string, object>();
                Cache.Add(folder, _cache);
            }

            if (_cache.TryGetValue(localPath, out object _value))
            {
                value = _value as T;
                return value != null;
            }

            if (TryLoad($"{folder}/{localPath}", out _value, preferMod, assetFolder, modFolder))
            {
                value = _value as T;
                _cache.Add(localPath, value);
                return value != null;
            }

            value = default(T);
            return false;
        }
        #endregion
    }
}