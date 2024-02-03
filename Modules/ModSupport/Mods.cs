using System.Collections.Generic;

namespace Cutulu
{
    public static class Mods
    {
        #region Load Mods           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public const string ModDirectory = $"{IO.USER_PATH}Mods/";
        public const string ModConfigEnding = ".mod";

        private static List<Mod> _loaded;

        /// <summary>
        /// Returns all loaded mod files. If no mods have been loaded yet they will here.
        /// </summary>
        public static List<Mod> Loaded
        {
            get
            {
                // Validate list
                if (_loaded == null)
                {
                    // Create new list
                    _loaded = new();

                    // Read directory names
                    string[] directories = ModDirectory.GetDirectories();

                    // Validate directory array
                    if (directories.NotEmpty())
                    {
                        // Iterate through directory names
                        for (int i = 0; i < directories.Length; i++)
                        {
                            // Read file names
                            string[] files = $"{ModDirectory}{directories[i]}".GetFiles();

                            // Validate file array
                            if (files.NotEmpty())
                            {
                                // Iterate through file names
                                for (int h = 0; h < files.Length; h++)
                                {
                                    // Validate file type
                                    if (files[i].EndsWith(ModConfigEnding))
                                    {
                                        // Try load mod
                                        if (Mod.TryLoad($"{ModDirectory}{directories[i]}/{files[i]}", out Mod mod))
                                        {
                                            // Assign directory path
                                            mod.DirectoryPath = $"{ModDirectory}{directories[i]}/";

                                            // Register mod
                                            _loaded.Add(mod);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Return mods
                return _loaded;
            }
        }

        /// <summary>
        /// Careful. This will unload any mods already loaded.
        /// <br/> It would be a better way to restart the application.
        /// </summary>
        public static void ReloadMods()
        {
            _loaded = null;
        }
        #endregion
    }

    #region Configuration File      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public struct ModConfig
    {
        public string Version { get; set; }
        public string Name { get; set; }
    }
    #endregion

    #region Mod Instance            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class Mod
    {
        public readonly string ConfigPath;
        public readonly ModConfig Config;
        public string DirectoryPath;

        private Mod(ref ModConfig config, ref string configPath)
        {
            ConfigPath = configPath;
            Config = config;
        }

        /// <summary>
        /// Try loading a Mod by finding its config file
        /// </summary>
        public static bool TryLoad(string configPath, out Mod mod)
        {
            if (IO.Exists(configPath) == false)
            {
                Debug.LogError($"No mod config file found at '{configPath}'");

                mod = null;
                return false;
            }

            try
            {
                string json = IO.Read(configPath);
                ModConfig config = json.json<ModConfig>();

                if (config.Name.IsEmpty())
                {
                    mod = null;
                    return false;
                }

                mod = new(ref config, ref configPath);
                return true;
            }

            catch
            {
                mod = null;
                return true;
            }
        }
    }
    #endregion
}