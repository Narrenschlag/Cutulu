namespace Cutulu.Core
{
    using Godot;

    using ACCESS = Godot.DirAccess;

    public partial struct Directory
    {
        public readonly string GodotLocal;
        public readonly string FileSystem;

        public Directory(string _path = "res://file.txt", bool _mkDir = true)
        {
            FileSystem = ProjectSettings.GlobalizePath(_path = _path.TrimToDirectory());
            GodotLocal = ProjectSettings.LocalizePath(FileSystem);

            if (_mkDir) Create();
        }

        public Directory()
        {
            GodotLocal = "res://";
            FileSystem = ProjectSettings.GlobalizePath(GodotLocal);
        }

        public bool Exists() => ACCESS.DirExistsAbsolute(FileSystem);

        public Error Delete()
        {
            return Exists() ? ACCESS.RemoveAbsolute(FileSystem) : Error.Ok;
        }

        public Error Create()
        {
            return Exists() ? Error.Ok : ACCESS.MakeDirAbsolute(FileSystem);
        }

        public Directory[] GetSubDirectories()
        {
            Directory[] _array = null;

            if (Exists())
            {
                var _subs = ACCESS.GetDirectoriesAt(FileSystem);

                if (_subs != null)
                {
                    _array = new Directory[_subs.Length];

                    for (int i = 0; i < _subs.Length; i++)
                    {
                        _array[i] = new(FileSystem + _subs[i] + '/');
                    }
                }
            }

            return _array ?? [];
        }

        public File[] GetSubFiles()
        {
            File[] _array = null;

            if (Exists())
            {
                var _subs = ACCESS.GetFilesAt(FileSystem);

                if (_subs != null)
                {
                    _array = new File[_subs.Length];

                    for (int i = 0; i < _subs.Length; i++)
                    {
                        _array[i] = new(FileSystem + _subs[i]);
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
    }
}