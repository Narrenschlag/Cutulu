namespace Cutulu.Core;

public static class Path
{
    public static string Combine(params string[] paths)
    => System.IO.Path.Combine(paths);

    public static string GetRelativePath(string from, string to)
    => System.IO.Path.GetRelativePath(from, to);

    public static string GetDirectoryName(string path)
    => System.IO.Path.GetDirectoryName(path);

    public static string GetFullPath(string path)
    => System.IO.Path.GetFullPath(path);
}