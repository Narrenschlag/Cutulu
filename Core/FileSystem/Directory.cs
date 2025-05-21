namespace Cutulu.Core
{
    using Godot;

    using ACCESS = Godot.DirAccess;

    public readonly partial struct Directory
    {
        public readonly string SystemPath;
        public readonly string GodotPath;

        public Directory(string _path = "res://file.txt", bool _mkDir = true)
        {
            SystemPath = ProjectSettings.GlobalizePath(_path.TrimToDirectory());
            GodotPath = ProjectSettings.LocalizePath(SystemPath);

            if (_mkDir) Create();
        }

        public Directory()
        {
            GodotPath = "res://";
            SystemPath = ProjectSettings.GlobalizePath(GodotPath);
        }

        public readonly bool Exists() => ACCESS.DirExistsAbsolute(SystemPath);

        public readonly Error Delete()
        {
            return Exists() ? ACCESS.RemoveAbsolute(SystemPath) : Error.Ok;
        }

        public readonly Error Create()
        {
            return Exists() ? Error.Ok : ACCESS.MakeDirAbsolute(SystemPath);
        }

        public readonly Directory[] GetSubDirectories()
        {
            Directory[] _array = null;

            if (Exists())
            {
                var _subs = ACCESS.GetDirectoriesAt(SystemPath);

                if (_subs != null)
                {
                    _array = new Directory[_subs.Length];

                    for (int i = 0; i < _subs.Length; i++)
                    {
                        _array[i] = new(SystemPath + _subs[i] + '/');
                    }
                }
            }

            return _array ?? [];
        }

        public readonly File[] GetSubFiles()
        {
            File[] _array = null;

            if (Exists())
            {
                var _subs = ACCESS.GetFilesAt(SystemPath);

                if (_subs != null)
                {
                    _array = new File[_subs.Length];

                    for (int i = 0; i < _subs.Length; i++)
                    {
                        _array[i] = new(SystemPath + _subs[i]);
                    }
                }
            }

            return _array ?? [];
        }
    }

    public static partial class Directoryf
    {
        public static string TrimToDirectory(this string path) => TrimToDirectory(path, '\\', '/');

        public static string TrimToDirectory(this string path, params char[] chars)
        {
            if (path.IsEmpty() || chars.IsEmpty()) return path;

            var contains = false;
            for (int i = 0; i < chars.Length; i++)
            {
                if (path.Contains(chars[i]) == false) continue;

                contains = true;
                break;
            }

            return contains ? path.TrimEndUntil(chars) : path;
        }

        public static bool PathExists(this string _path) => ACCESS.DirExistsAbsolute(ProjectSettings.GlobalizePath(_path.Trim()));

        public static void DeletePath(this string _path) => ACCESS.RemoveAbsolute(ProjectSettings.GlobalizePath(_path.Trim()));
    }
}