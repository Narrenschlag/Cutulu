using Godot;

namespace Cutulu
{
    public static class FileSystem
    {
        /// <summary>
        /// Opens file dialogue to open a file.
        /// Upon selection the function OnFileOpened(string path) is called on the parent node.
        /// </summary>
        public static void OpenFileDialogue(this string directory, Node parent, string outputFunctionName = "OnFileOpened", params string[] fileEndings)
        {
            // Create a new FileDialog instance
            FileDialog fileDialog = new()
            {
                Filters = fileEndings,
                FileMode = FileDialog.FileModeEnum.OpenFile,
                Access = FileDialog.AccessEnum.Filesystem,
                CurrentDir = directory,
                UseNativeDialog = true,
            };
            parent.AddChild(fileDialog);

            // Connect the file_selected signal to a method in your class
            fileDialog.Connect("file_selected", parent, outputFunctionName);

            // Show the file dialog
            fileDialog.PopupCentered();
        }

        /// <summary>
        /// Opens file dialogue to save a file.
        /// Upon saving the function OnFileSaved(string path) is called on the parent node.
        /// </summary>
        public static void SaveFileDialogue(this string directory, Node parent, string outputFunctionName = "OnFileSaved", params string[] fileEndings)
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
            fileDialog.Connect("file_selected", parent, outputFunctionName);

            // Show the file dialog
            fileDialog.PopupCentered();
        }
    }
}