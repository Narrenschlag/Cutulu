#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using System;
    using Godot;

    public static class FileSystem
    {
        /// <summary>
        /// Opens file dialogue to open a file.
        /// Upon selection the function OnFileOpened(string path) is called on the parent node.
        /// </summary>
        public static void OpenFileDialogue(this string directory, Node parent, Action<string[]> action, bool singleFile, params string[] fileEndings)
        {
            FixFilters(fileEndings);

            // Create a new FileDialog instance
            FileDialog fileDialog = new()
            {
                Filters = fileEndings,
                FileMode = singleFile ? FileDialog.FileModeEnum.OpenFile : FileDialog.FileModeEnum.OpenFiles,
                Access = FileDialog.AccessEnum.Filesystem,
                CurrentDir = directory,
                UseNativeDialog = true,
            };
            parent.AddChild(fileDialog);

            // Connect the file_selected signal to a method in your class
            fileDialog.FileSelected += (string path) => action?.Invoke(new[] { path });
            fileDialog.FilesSelected += (string[] paths) => action?.Invoke(paths);

            // Show the file dialog
            fileDialog.PopupCentered();
        }

        /// <summary>
        /// Opens file dialogue to save a file.
        /// Upon saving the function OnFileSaved(string path) is called on the parent node. [*.txt for example]
        /// </summary>
        public static void SaveFileDialogue(this string directory, Node parent, Action<string> action, params string[] fileEndings)
        {
            FixFilters(fileEndings);

            // Create a new FileDialog instance
            FileDialog fileDialog = new()
            {
                Filters = fileEndings,
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Access = FileDialog.AccessEnum.Filesystem,
                CurrentDir = directory,
                UseNativeDialog = true,
            };
            parent.AddChild(fileDialog);

            // Connect the file_selected signal to a method in your class
            fileDialog.FileSelected += (string path) => action?.Invoke(path);

            // Show the file dialog
            fileDialog.PopupCentered();
        }

        private static void FixFilters(string[] filters)
        {
            if (filters.IsEmpty()) return;

            for (int i = 0; i < filters.Length; i++)
            {
                filters[i] = filters[i].Trim();

                if (filters[i][0] == '.') filters[i] = $"*{filters[i]}";
                else if (filters[i].StartsWith("*.") == false) filters[i] = $"*.{filters[i]}";
            }
        }

        public static string AsDirectory(this string _directory)
        {
            if (_directory.EndsWith("//") || _directory.EndsWith("\\\\")) return _directory;
            else return $"{_directory.TrimEnd('/').TrimEnd('\\')}/";
        }
    }
}
#endif