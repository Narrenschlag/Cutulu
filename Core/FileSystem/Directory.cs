namespace Cutulu.Core;

using System.Collections.Generic;
using System;

#if GODOT4_0_OR_GREATER
using Godot;
using ACCESS = Godot.DirAccess;
#else
using System.IO;
#endif

/// <summary>
/// Cross-platform file directory abstraction for both Godot and .NET.
/// </summary>
public readonly partial struct Directory
{
    public readonly string SystemPath;

#if GODOT4_0_OR_GREATER
    public readonly string GodotPath;
#endif

    public Directory(string path = "res://", bool createIfMissing = true)
    {
        path = path.TrimToDirectory();

#if GODOT4_0_OR_GREATER
        SystemPath = ProjectSettings.GlobalizePath(path);
        GodotPath = ProjectSettings.LocalizePath(SystemPath);
#else
        SystemPath = Path.GetFullPath(path);
#endif

        if (createIfMissing) MakeDir();
    }

    public Directory()
        : this("res://", true) { }

    /// <summary>Returns true if the directory exists.</summary>
    public bool Exists()
    {
#if GODOT4_0_OR_GREATER
        return ACCESS.DirExistsAbsolute(SystemPath);
#else
        return Directory.Exists(SystemPath);
#endif
    }

    /// <summary>Creates the directory if it doesn't exist.</summary>
    public bool MakeDir()
    {
        try
        {
            if (Exists() == false) System.IO.Directory.CreateDirectory(SystemPath);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Deletes the directory if it exists.</summary>
    public bool Delete()
    {
#if GODOT4_0_OR_GREATER
        return !Exists() || ACCESS.RemoveAbsolute(SystemPath) == Error.Ok;
#else
            try
            {
                if (Exists())
                    System.IO.Directory.Delete(SystemPath, true);
                return true;
            }
            catch { return false; }
#endif
    }

    /// <summary>Returns all subdirectories inside this directory.</summary>
    public Directory[] GetSubDirectories()
    {
#if GODOT4_0_OR_GREATER
        if (!Exists()) return [];

        var subs = ACCESS.GetDirectoriesAt(SystemPath);
        if (subs == null) return [];

        var result = new Directory[subs.Length];
        for (int i = 0; i < subs.Length; i++)
            result[i] = new(SystemPath + "/" + subs[i] + "/");

        return result;
#else
            if (!Exists()) return [];

            var dirs = System.IO.Directory.GetDirectories(SystemPath);
            var result = new Directory[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
                result[i] = new(dirs[i]);

            return result;
#endif
    }

    /// <summary>Returns all files inside this directory.</summary>
    public File[] GetSubFiles()
    {
#if GODOT4_0_OR_GREATER
        if (!Exists()) return [];

        var files = ACCESS.GetFilesAt(SystemPath);
        var result = new File[files.Length];
        for (int i = 0; i < files.Length; i++)
            result[i] = new(SystemPath + "/" + files[i]);

        return result;
#else
            if (!Exists()) return [];

            var files = System.IO.Directory.GetFiles(SystemPath);
            var result = new File[files.Length];
            for (int i = 0; i < files.Length; i++)
                result[i] = new(files[i]);

            return result;
#endif
    }

    /// <summary>Returns all files inside this directory filtered by extension(s).</summary>
    public File[] GetSubFiles(params string[] fileTypes)
    {
        if (fileTypes == null || fileTypes.Length == 0)
            return GetSubFiles();

        var list = new List<File>();

#if GODOT4_0_OR_GREATER
        var subs = ACCESS.GetFilesAt(SystemPath);
        if (subs != null)
        {
            foreach (var file in subs)
            {
                foreach (var ext in fileTypes)
                {
                    if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(new File(SystemPath + "/" + file));
                        break;
                    }
                }
            }
        }
#else
            var subs = System.IO.Directory.GetFiles(SystemPath);
            foreach (var file in subs)
            {
                foreach (var ext in fileTypes)
                {
                    if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(new File(file));
                        break;
                    }
                }
            }
#endif
        return [.. list];
    }
}

public static partial class Directoryf
{
    public static string TrimToDirectory(this string path) => TrimToDirectory(path, '\\', '/');

    public static string TrimToDirectory(this string path, params char[] chars)
    {
        if (string.IsNullOrEmpty(path) || chars.IsEmpty()) return path;

        var index = path.LastIndexOfAny(chars);
        return index >= 0 ? path[..(index + 1)] : path;
    }

    public static bool PathExists(this string path)
    {
#if GODOT4_0_OR_GREATER
        return ACCESS.DirExistsAbsolute(ProjectSettings.GlobalizePath(path.Trim()));
#else
            return System.IO.Directory.Exists(path) || System.IO.File.Exists(path);
#endif
    }

    public static void DeletePath(this string path)
    {
#if GODOT4_0_OR_GREATER
        ACCESS.RemoveAbsolute(ProjectSettings.GlobalizePath(path.Trim()));
#else
            var clean = path.Trim();
            if (System.IO.Directory.Exists(clean))
                System.IO.Directory.Delete(clean, true);
            else if (System.IO.File.Exists(clean))
                System.IO.File.Delete(clean);
#endif
    }
}