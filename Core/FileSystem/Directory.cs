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
/// Supports PCK filesystem in exported builds.
/// </summary>
public readonly partial struct Directory
{
    public readonly string SystemPath;

#if GODOT4_0_OR_GREATER
    public readonly string GodotPath;
#endif

    public Directory(string path = "res://", bool createIfMissing = true)
    {
#if GODOT4_0_OR_GREATER
        // For Godot virtual paths, use them directly without modification
        if (path.StartsWith("res://") || path.StartsWith("user://"))
        {
            // Ensure trailing slash
            GodotPath = path.EndsWith('/') ? path : path + "/";
            SystemPath = ProjectSettings.GlobalizePath(GodotPath);
        }
        else
        {
            // Only trim non-Godot paths
            if (!path.EndsWith('/') && !path.EndsWith('\\'))
                path = path.TrimToDirectory();

            SystemPath = ProjectSettings.GlobalizePath(path);
            GodotPath = ProjectSettings.LocalizePath(SystemPath);
        }
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
        // Try Godot virtual filesystem first (works in PCK)
        if (GodotPath.StartsWith("res://"))
        {
            using var dir = ACCESS.Open(GodotPath);
            return dir != null;
        }
        // Fall back to system path
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

        // Use Godot virtual filesystem if it's a res:// path (supports PCK)
        if (GodotPath.StartsWith("res://") || GodotPath.StartsWith("user://"))
        {
            using var dir = ACCESS.Open(GodotPath);
            if (dir == null) return [];

            var list = new List<Directory>();
            dir.ListDirBegin();
            string dirName = dir.GetNext();

            while (!string.IsNullOrEmpty(dirName))
            {
                if (dirName != "." && dirName != ".." && dir.CurrentIsDir())
                {
                    // Manually construct the full path with trailing slash
                    string fullPath = GodotPath.TrimEnd('/') + "/" + dirName + "/";
                    list.Add(new(fullPath, false));
                }
                dirName = dir.GetNext();
            }

            dir.ListDirEnd();
            return [.. list];
        }

        // Fall back to system path method
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

    /// <summary>Returns all files inside this directory with full paths.</summary>
    public File[] GetSubFiles()
    {
#if GODOT4_0_OR_GREATER
        if (!Exists()) return [];

        // Use Godot virtual filesystem if it's a res:// path (supports PCK)
        if (GodotPath.StartsWith("res://") || GodotPath.StartsWith("user://"))
        {
            using var dir = ACCESS.Open(GodotPath);
            if (dir == null) return [];

            var list = new List<File>();
            dir.ListDirBegin();
            string fileName = dir.GetNext();

            while (!string.IsNullOrEmpty(fileName))
            {
                if (fileName != "." && fileName != ".." && !dir.CurrentIsDir())
                {
                    // Remove .remap suffix if present
                    string cleanFileName = fileName;
                    if (cleanFileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase))
                    {
                        cleanFileName = cleanFileName.Substring(0, cleanFileName.Length - 6);
                    }

                    string fullPath = GodotPath.TrimEnd('/') + "/" + cleanFileName;
                    list.Add(new File(fullPath));
                }
                fileName = dir.GetNext();
            }

            dir.ListDirEnd();
            return [.. list];
        }

        // Fall back to system path method
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

    /// <summary>Returns all files inside this directory filtered by extension(s) with full paths.</summary>
    public File[] GetSubFiles(params string[] fileTypes)
    {
        if (fileTypes == null || fileTypes.Length == 0)
            return GetSubFiles();

        var list = new List<File>();

#if GODOT4_0_OR_GREATER
        if (GodotPath.StartsWith("res://") || GodotPath.StartsWith("user://"))
        {
            using var dir = ACCESS.Open(GodotPath);
            if (dir == null) return [];

            dir.ListDirBegin();
            string fileName = dir.GetNext();

            while (!string.IsNullOrEmpty(fileName))
            {
                if (fileName != "." && fileName != ".." && !dir.CurrentIsDir())
                {
                    foreach (var ext in fileTypes)
                    {
                        // Check both original extension and .remap version
                        bool matches = fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ||
                                       fileName.EndsWith(ext + ".remap", StringComparison.OrdinalIgnoreCase);

                        if (matches)
                        {
                            // Always use the path WITHOUT .remap - Godot handles this internally
                            string cleanFileName = fileName;
                            if (cleanFileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase))
                            {
                                cleanFileName = cleanFileName.Substring(0, cleanFileName.Length - 6);
                            }

                            string fullPath = GodotPath.TrimEnd('/') + "/" + cleanFileName;
                            list.Add(new File(fullPath));
                            break;
                        }
                    }
                }
                fileName = dir.GetNext();
            }

            dir.ListDirEnd();
            return [.. list];
        }

        // Fall back to system path method
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
    public static string TrimToDirectory(this string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        // Don't modify if it already ends with a directory separator
        if (path.EndsWith('/') || path.EndsWith('\\')) return path;

        // For Godot paths (res://, user://), preserve the protocol
        if (path.StartsWith("res://") || path.StartsWith("user://"))
        {
            // Find last slash
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash > 0)
            {
                return path[..(lastSlash + 1)];
            }
            // If no slash found after protocol, add trailing slash
            return path + "/";
        }

        // For system paths, use System.IO but ensure forward slashes for Godot
        var dir = System.IO.Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir)) return path;
        return dir.Replace("\\", "/") + "/";
    }

    public static bool PathExists(this string path)
    {
#if GODOT4_0_OR_GREATER
        var cleaned = path.Trim();

        // Check virtual filesystem first (PCK support)
        if (cleaned.StartsWith("res://") || cleaned.StartsWith("user://"))
        {
            // Try as directory
            using var dir = DirAccess.Open(cleaned);
            if (dir != null) return true;

            // Try as file
            return FileAccess.FileExists(cleaned);
        }

        // Fall back to system path
        return ACCESS.DirExistsAbsolute(ProjectSettings.GlobalizePath(cleaned));
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