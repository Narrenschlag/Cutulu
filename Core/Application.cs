namespace Cutulu.Core
{
    using Godot;

    public static class Application
    {
        /// <summary>
        /// Close the application.
        /// </summary>
        public static void Quit() => Nodef.Main.GetTree().Quit();

        /// <summary>
        /// Open url in web browser.
        /// </summary>
        public static void OpenUrl(this string url, string requiredStart = "https://")
        {
            if (requiredStart.IsEmpty() || url.StartsWith(requiredStart))
                OS.ShellOpen(url.Trim());
        }

        /// <summary>
        /// Define new title for application window.
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
    }
}