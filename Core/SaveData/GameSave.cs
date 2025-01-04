using System.Collections.Generic;
using System.Text.Json;

namespace Cutulu.Core
{
    /// <summary>
    /// Static, Json-based save system designed for smaller games to easily implement saving.
    /// <br/>May be used for middle sized games too. Larger save files may result in slow saving/loading performance.
    /// <br/>For more performance use a custom struct that contains all the data and save it using byte write/read utility.
    /// </summary>
    public static class GameSave
    {
        #region Frontend      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Last SaveFileName assigned when file was loaded/created
        /// </summary>
        public static string LastSaveFileName { private set; get; }

        /// <summary>
        /// Creates new save file. Does not save it.
        /// To save it, use GameSave.SaveGame().
        /// </summary>
        public static bool Create(string saveFileName, bool doNotOverride = false)
        {
            // Stop if no saveFileName is given or not overriding and already existing
            if (saveFileName.IsEmpty() || (doNotOverride && Exists(saveFileName)))
            {
                return false;
            }

            // Remember last save file name
            LastSaveFileName = saveFileName;

            return true;
        }

        /// <summary>
        /// Loads game file from last loaded/created SaveFileName
        /// </summary>
        public static bool LoadGame() => LoadGame(LastSaveFileName);

        /// <summary>
        /// Loads game file from given SaveFileName
        /// </summary>
        public static bool LoadGame(string saveFileName)
        {
            // Stop if no saveFileName or not existing
            if (saveFileName.IsEmpty() || Exists(saveFileName) == false)
            {
                return false;
            }

            // Remember last save file name
            LastSaveFileName = saveFileName;

            // Load json
            string json = SaveFilePath(saveFileName).ReadString();

            // Assign to local
            Local = json.json<Dictionary<string, object>>();

            return true;
        }

        /// <summary>
        /// Saves game with the last loaded/created SaveFileName
        /// </summary>
        public static void SaveGame() => SaveGame(LastSaveFileName);

        /// <summary>
        /// Saves file to given file SaveFileName
        /// </summary>
        public static void SaveGame(string saveFileName)
        {
            // Return if no name is given
            if (saveFileName.IsEmpty())
            {
                return;
            }

            // Create empty local if is null
            Local ??= new();

            // Write to file
            SaveFilePath(saveFileName).WriteString(Local.json());
        }

        /// <summary>
        /// Deletes save file if existing
        /// </summary>
        public static void Delete(string saveFileName)
        {
            // Return if no name is given
            if (saveFileName.IsEmpty())
            {
                return;
            }

            // Delete file if existing
            IO.DeleteFile(SaveFilePath(saveFileName));
        }

        /// <summary>
        /// Returns if save file is existing already
        /// </summary>
        public static bool Exists(string saveFileName) => SaveFilePath(saveFileName).Exists();

        /// <summary>
        /// Adds value if key is not added yet
        /// </summary>
        public static bool TryAdd<V>(string key, V value) => Local.TryAdd(key, value);

        /// <summary>
        /// Sets value in save file
        /// </summary>
        public static void SetValue<V>(string key, V value) => Local.Set(key, value);

        /// <summary>
        /// Removes value from save file
        /// </summary>
        public static void RemoveValue(string key) => Local.TryRemove(key);

        /// <summary>
        /// Returns if the key is contained in the save file
        /// </summary>
        public static bool Contains(string key) => (Local ??= new()).ContainsKey(key);

        /// <summary>
        /// Returns value, if key isn't in save file returns default value
        /// </summary>
        public static V GetValue<V>(string key, V defaultValue)
        {
            if ((Local ??= new()).TryGetValue(key, out object value))
            {
                return safeGetValue<V>(key, ref value);
            }

            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Tries to get value, returns false if none were found
        /// </summary>
        public static bool TryGetValue<V>(string key, out V value)
        {
            if ((Local ??= new()).TryGetValue(key, out object _value))
            {
                value = safeGetValue<V>(key, ref _value);
                return true;
            }

            else
            {
                value = default;
                return false;
            }
        }

        #region Path and File Ending Management
        private const string DefaultSaveLocation = $"{IO.USER_PATH}save_games/";
        private const string DefaultSaveFileEnding = ".cutulu";
        private static string saveFileEnding;
        private static string saveLocation;

        /// <summary>
        /// Custom save file endings (default: .cutulu)
        /// </summary>
        public static string SaveFileEnding
        {
            set => saveFileEnding = value;
            get
            {
                if (saveFileEnding.IsEmpty())
                {
                    SaveFileEnding = DefaultSaveFileEnding;
                }

                return saveFileEnding;
            }
        }

        /// <summary>
        /// Custom save location (default: user://save_games/)
        /// </summary>
        public static string SaveLocation
        {
            set => saveLocation = value;
            get
            {
                if (saveLocation.IsEmpty())
                {
                    SaveLocation = DefaultSaveLocation;
                }

                return saveLocation;
            }
        }

        /// <summary>
        /// Returns save file path with given SaveFileName, using the SaveLocation and the SaveFileEnding.
        /// </summary>
        public static string SaveFilePath(string saveFileName) => $"{SaveLocation}{saveFileName}{(saveFileName.EndsWith(SaveFileEnding) ? "" : SaveFileEnding)}";
        #endregion
        #endregion

        #region Backend       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static Dictionary<string, object> Local;

        private static V safeGetValue<V>(string key, ref object value)
        {
            // Decypher json element if it is one
            if (value is JsonElement)
            {
                if (typeof(V) == typeof(string))
                {
                    value = value.ToString();
                }

                else
                {
                    value = value.ToString().json<V>();
                }

                // Assign value with type to local
                Local.Set(key, value);
            }

            return (V)value;
        }
        #endregion
    }
}