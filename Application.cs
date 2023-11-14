using Godot;

namespace Cutulu
{
    public static class Application
    {
        public static void Quit() => Core.Main.GetTree().Quit();

        public static void OpenUrl(this string url, string requiredStart = "https://")
        {
            if (requiredStart.IsEmpty() || url.StartsWith(requiredStart))
                OS.ShellOpen(url.Trim());
        }
    }
}