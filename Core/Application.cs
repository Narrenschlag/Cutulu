namespace Cutulu.Core;

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
#endif

    public static bool IsExecutablePath(this string path)
    {
        if (OSExecFileExtension.IsEmpty()) return true;
        else return path.EndsWith(OSExecFileExtension, StringComparison.OrdinalIgnoreCase);
    }

    public static readonly OperatingSystem OperatingSystem = Environment.OSVersion;

    public static readonly OS_TYPE OperatingSystemType =
        OperatingSystem.IsMacCatalyst() ? OS_TYPE.MAC_CATALYST :
        OperatingSystem.IsWindows() ? OS_TYPE.WINDOWS :
        OperatingSystem.IsFreeBSD() ? OS_TYPE.FREEBSD :
        OperatingSystem.IsAndroid() ? OS_TYPE.ANDROID :
        OperatingSystem.IsBrowser() ? OS_TYPE.WEB :
        OperatingSystem.IsWatchOS() ? OS_TYPE.WATCH :
        OperatingSystem.IsLinux() ? OS_TYPE.LINUX :
        OperatingSystem.IsMacOS() ? OS_TYPE.MAC :
        OperatingSystem.IsWasi() ? OS_TYPE.WASI :
        OperatingSystem.IsTvOS() ? OS_TYPE.TV :
        OperatingSystem.IsIOS() ? OS_TYPE.IOS :
        OS_TYPE.UNKNOWN;

    public static readonly OS_CATEGORY OperatingSystemCategory =
        OperatingSystemType switch
        {
            OS_TYPE.MAC_CATALYST => OS_CATEGORY.DESKTOP,
            OS_TYPE.WINDOWS => OS_CATEGORY.DESKTOP,
            OS_TYPE.FREEBSD => OS_CATEGORY.DESKTOP,
            OS_TYPE.LINUX => OS_CATEGORY.DESKTOP,
            OS_TYPE.MAC => OS_CATEGORY.DESKTOP,
            OS_TYPE.ANDROID => OS_CATEGORY.MOBILE,
            OS_TYPE.IOS => OS_CATEGORY.MOBILE,
            OS_TYPE.WATCH => OS_CATEGORY.MOBILE,
            OS_TYPE.TV => OS_CATEGORY.MOBILE,
            OS_TYPE.WEB => OS_CATEGORY.WEB,
            OS_TYPE.WASI => OS_CATEGORY.WEB,
            _ => OS_CATEGORY.UNKNOWN,
        };

    /// <summary>
    /// Returns main executable file ending based on current runtime OS.
    /// </summary>
    public static readonly string OSExecFileExtension =
#if GODOT4_0_OR_GREATER
    OS.GetName() switch
    {
        "Windows" => ".exe",
        "X11" => ".x86_64",
        "macOS" => ".app",
        "Android" => ".apk",
        "iOS" => ".ipa",
        "Web" => ".html",
        _ => "",
    };
#else
    OperatingSystemType switch
    {
        OS_TYPE.MAC_CATALYST => ".dmg",
        OS_TYPE.WINDOWS => ".exe",
        OS_TYPE.LINUX => ".x86_64",
        OS_TYPE.ANDROID => ".apk",
        OS_TYPE.MAC => ".app",
        OS_TYPE.IOS => ".ipa",
        OS_TYPE.WEB => ".html",
        _ => "",
    };
#endif
}

public enum OS_CATEGORY : byte
{
    UNKNOWN = 0,
    DESKTOP,
    MOBILE,
    WEB,

    WATCH,
    TV,
}

public enum OS_TYPE : byte
{
    UNKNOWN = 0,

    ANDROID,
    IOS,

    WINDOWS,
    FREEBSD,
    LINUX,
    MAC,

    MAC_CATALYST,
    WATCH,
    TV,

    WEB,
    WASI, // WebAssembly
}