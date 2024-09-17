using Godot;

namespace Cutulu
{
    public static class Application
    {
        /// <summary>
        /// Close the application.
        /// </summary>
        public static void Quit() => Core.Main.GetTree().Quit();

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

        public static bool Fullscreen
        {
            get => DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
            set => SetFullscreen(value);
        }

        private static void SetFullscreen(bool enabled)
        {
            if (enabled && Fullscreen == false)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            }

            else if (enabled == false && Fullscreen)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
            }
        }
    }
}