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

        public static void SetWindowSize(Vector2I size) => DisplayServer.WindowSetSize(size);
        public static Vector2I GetWindowSize() => DisplayServer.WindowGetSize();

        public static Vector2I GetScreenSize() => DisplayServer.ScreenGetSize();

        public static void CenterWindow()
        {
            var window = Nodef.Main.GetWindow();

            var center = DisplayServer.ScreenGetPosition() + GetScreenSize() / 2;
            var size = window.GetSizeWithDecorations();

            window.Position = center - size / 2;
        }

        public static void SetResolution(Vector2I resolution, bool enableWindow = true)
        {
            if (enableWindow)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            }

            SetWindowSize(resolution);
            CenterWindow();
        }
    }
}