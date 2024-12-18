using System;
using Godot;

namespace Cutulu
{
    public static class FileSystem
    {
        /// <summary>
        /// Opens file dialogue to open a file.
        /// Upon selection the function OnFileOpened(string path) is called on the parent node.
        /// </summary>
        public static void OpenFileDialogue(this string directory, Node parent, Action<string[]> action, bool singleFile, params string[] fileEndings)
        {
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
        /// Upon saving the function OnFileSaved(string path) is called on the parent node.
        /// </summary>
        public static void SaveFileDialogue(this string directory, Node parent, Action<string> action, params string[] fileEndings)
        {
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
    }
}