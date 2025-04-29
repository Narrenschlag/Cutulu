namespace Cutulu.Core
{
    using Godot;

    using ACCESS = Godot.FileAccess;
    using FLAGS = Godot.FileAccess.ModeFlags;

    public partial struct File
    {
        public readonly string GodotLocal;
        public readonly string FileSystem;

        private ACCESS Access { get; set; }

        public File(string _path = "res://file.txt")
        {
            FileSystem = ProjectSettings.GlobalizePath(_path = _path.Trim());
            GodotLocal = ProjectSettings.LocalizePath(FileSystem);
        }

        #region Main functions

        public bool Exists() => ACCESS.FileExists(FileSystem);

        public bool IsOpen() => Access != null;

        public FileAccess Open(FLAGS _flags, bool _reopen = true)
        {
            if (IsOpen() == false || _reopen)
            {
                Close();

                Access = ACCESS.Open(FileSystem, _flags);
            }

            return Access;
        }

        public void Close()
        {
            Access?.Close();
            Access = null;
        }

        public Error Delete()
        {
            return Exists() ? DirAccess.RemoveAbsolute(FileSystem) : Error.Ok;
        }

        public ulong GetFileSize()
        {
            var _length = GetFileSizeRaw();
            Close();

            return _length;
        }

        public byte[] Read()
        {
            var _buffer = ReadRaw(GetFileSizeRaw());
            Close();

            return _buffer;
        }

        public byte[] Read(ulong _length)
        {
            var _buffer = ReadRaw(_length);
            Close();

            return _buffer;
        }

        public void Write(byte[] _buffer)
        {
            if (_buffer.IsEmpty()) Delete();

            else
            {
                Open(FLAGS.Write, true);
                WriteRaw(_buffer);
                FlushRaw();
                Close();
            }
        }

        public Directory[] GetSiblingDirectories()
        {
            return new Directory(FileSystem).GetSubDirectories();
        }

        public File[] GetSiblingFiles()
        {
            var _siblings = new Directory(FileSystem).GetSubFiles();

            if (_siblings.Length > 1)
            {
                var _files = new File[_siblings.Length - 1];

                for (int i = 0, k = 0; i < _siblings.Length; i++, k++)
                {
                    if (i == k && _siblings[i].FileSystem == FileSystem) i++;
                    else _files[k] = _siblings[i];
                }

                return _files;
            }

            return [];
        }

        #endregion

        #region Raw functions

        public ulong GetFileSizeRaw()
        {
            return Open(FLAGS.Read, false)?.GetLength() ?? default;
        }

        public byte[] ReadRaw(ulong _length)
        {
            return Open(FLAGS.Read, false)?.GetBuffer((long)_length) ?? [];
        }

        public void WriteRaw(byte[] _buffer)
        {
            if (_buffer.IsEmpty()) return;

            Open(FLAGS.Write, false)?.StoreBuffer(_buffer);
        }

        public void FlushRaw()
        {
            Open(FLAGS.Write, false)?.Flush();
        }

        #endregion

        #region String functions

        public void WriteString(string _string)
        {
            Write(_string.IsEmpty() ? null : _string.Encode());
        }

        public string ReadString()
        {
            return Read().TryDecode(out string _string) && _string.NotEmpty() ? _string : string.Empty;
        }

        public string[] ReadStringLines(bool _include_empty_lines = false)
        {
            return ReadString().Split('\n', _include_empty_lines ? System.StringSplitOptions.TrimEntries : CONST.StringSplit) ?? [];
        }

        #endregion
    }

    public static partial class Filef
    {

    }
}