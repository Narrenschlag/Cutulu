namespace Cutulu.Core.Logging;

using System;

public static class Logging
{
    private static Logger Logger = null;

    public static void SetLogger(Logger logger) => Logger = logger;

    public static void Log(string text) => Logger?.Log(text);

    public static void Export() => Logger?.Write();

    static Logging()
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => OnCloseOrCrash();
        AppDomain.CurrentDomain.UnhandledException += (s, e) => OnCloseOrCrash();
    }

    /// <summary>
    /// If in Godot use
    /// public override void _Notification(int what)
    /// {
    ///    // This constant is defined on Node in Godot 4.x
    ///    if (what == NotificationWMCloseRequest)
    ///    {
    ///        // Flush or export your logs before the engine quits
    ///        Cutulu.Core.Logging.Logging.OnCloseOrCrash();
    ///
    ///        // Tell Godot to continue quitting
    ///        Application.Quit();
    ///    }
    ///}
    /// </summary>
    public static void OnCloseOrCrash()
    {
        if (Logger.NotNull()) Logger.Write();
    }
}