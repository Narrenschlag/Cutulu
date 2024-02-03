using Godot;

namespace Cutulu
{
    public static class Application
    {
        /// <summary>
        /// Close the application
        /// </summary>
        public static void Quit() => Core.Main.GetTree().Quit();

        /// <summary>
        /// Open url in web browser
        /// </summary>
        public static void OpenUrl(this string url, string requiredStart = "https://")
        {
            if (requiredStart.IsEmpty() || url.StartsWith(requiredStart))
                OS.ShellOpen(url.Trim());
        }
    }
}