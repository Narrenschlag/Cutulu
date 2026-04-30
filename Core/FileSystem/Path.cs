namespace Cutulu.Core;

public static class Path
{
    public static readonly char DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;

    public static string Combine(params string[] paths)
    => System.IO.Path.Combine(paths);

    public static string GetRelativePath(string from, string to)
    => System.IO.Path.GetRelativePath(from, to);

    public static string GetDirectoryName(string path)
    => System.IO.Path.GetDirectoryName(path);

    public static string GetFullPath(string path)
    => System.IO.Path.GetFullPath(path);

    public static string GetFileName(string path)
    => System.IO.Path.GetFileName(path);

    public static string GetExtension(string path)
    => System.IO.Path.GetExtension(path);
}