namespace Cutulu.Core
{
#if GODOT4_0_OR_GREATER
    using Godot;
#endif

    public static class Application
    {
#if GODOT4_0_OR_GREATER
        /// <summary>
        /// Close the application
        /// </summary>
        public static void Quit()
        {
            // Notify logger
            Logging.Logging.OnCloseOrCrash();

            // Close the application
            Nodef.Main.GetTree().Quit();
        }

        /// <summary>
        /// Open url in web browser
        /// </summary>
        public static void OpenUrl(this string url, string requiredStart = "https://")
        {
            if (requiredStart.IsEmpty() || url.StartsWith(requiredStart))
                OS.ShellOpen(url.Trim());
        }

        /// <summary>
        /// Define new title for application window
        /// </summary>
        public static void SetWindowTitle(string newTitle, int windowId = 0) => DisplayServer.WindowSetTitle(newTitle, windowId);

        /// <summary>
        /// Set or Get value of Clipboard
        /// </summary>
        public static string Clipboard
        {
            set => DisplayServer.ClipboardSet(value);
            get => DisplayServer.ClipboardGet();
        }

        /// <summary>
        /// Returns true if running in headless mode
        /// </summary>
        public static bool IsHeadless
        {
            get => DisplayServer.GetName() == "headless";
        }

        /// <summary>
        /// Returns true if any --script-args were provided
        /// <para>Schema: "command-to-start-application" --script-args "arg1 arg2 arg3"</para>
        /// </summary>
        public static bool TryGetCmdLineArgs(out string[] _args)
        {
            // Get all command line arguments
            return (_args = OS.GetCmdlineArgs()).NotEmpty();
        }

        /// <summary>
        /// Returns any --script-args if provided
        /// <para>Schema: "command-to-start-application" --script-args "arg1 arg2 arg3"</para>
        /// </summary>
        public static string[] GetCmdLineArgs() => TryGetCmdLineArgs(out var args) ? args ?? [] : [];

        /// <summary>
        /// Returns main executable file ending based on current runtime OS.
        /// </summary>
        public static string OSExecFileExtension => OS.GetName() switch
        {
            "Windows" => ".exe",
            "X11" => ".x86_64",
            "macOS" => ".app",
            "Android" => ".apk",
            "iOS" => ".ipa",
            "Web" => ".html",
            _ => "",
        };
#endif
    }
}