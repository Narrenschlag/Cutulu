#if GODOT4_0_OR_GREATER
namespace Cutulu.UI;

using System.Collections.Generic;
using Godot;
using Core;

[GlobalClass]
public partial class FileDrop : HBoxContainer
{
    [Export] public bool SingleFile { get; set; }
    [Export] public string[] Extensions { get; set; }

    [ExportGroup($"Visible UI")]
    [Export] public string FileNameFormat { get; set; } = "{path}";
    [Export] public string PlaceholderText { get; set; } = "<no_file>";
    [Export] private Button FileNameButton { get; set; }

    public readonly Notification<FileDrop> FileSelected = new();

    public File[] Files { get; set; }

    public bool HasFile() => Files.NotEmpty();

    public override void _EnterTree()
    {
        SetFiles(Files.IsEmpty() ? null : [.. Files]);

        GetWindow().FilesDropped += OnFilesDropped;

        if (FileNameButton.NotNull()) FileNameButton.Pressed += OpenFolder;
    }

    public override void _ExitTree()
    {
        GetWindow().FilesDropped -= OnFilesDropped;

        if (FileNameButton.NotNull()) FileNameButton.Pressed -= OpenFolder;
    }

    public void OpenFolder()
    {
        FileSystem.OpenFileDialogue("", this, OnFilesSelected, SingleFile, Extensions);
    }

    private void OnFilesDropped(string[] files)
    {
        if (this.IsMouseOver()) OnFilesSelected(files);
    }

    public void Preload(string[] files)
    {
        OnFilesSelected(files);
    }

    private void OnFilesSelected(string[] files)
    {
        if (files?.Length < 1) Debug.LogError("No files dropped");
        else Debug.Log($"Loaded {files.Length} files: \n- {string.Join("\n- ", files)}");

        var list = new List<File>(files.Size());

        if (files.NotEmpty())
            foreach (var path in files)
            {
                var file = new File(path);

                if (file?.Exists() ?? false) list.Add(file);
            }

        SetFiles(list);
    }

    public void SetFiles(List<File> files)
    {
        if (files.IsEmpty())
        {
            Files = null;
            SetFileNameText(PlaceholderText);
        }

        else if (files.Count == 1)
        {
            Files = [.. files];
            SetFileNameText(files[0].SystemPath);
        }

        else
        {
            if (SingleFile)
            {
                Debug.LogError("Only one file can be loaded");
                return;
            }

            Files = [.. files];
            SetFileNameText($"{files.Count} files");
        }
    }

    public void SetFileNameText(string text)
    {
        if (FileNameButton.NotNull())
            FileNameButton.Text = FileNameFormat.Replace("{path}", text);

        FileSelected.Invoke(this);
    }

    public void Clear() => SetFiles(null);
}
#endif