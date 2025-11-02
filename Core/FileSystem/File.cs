namespace Cutulu.Core;

#if GODOT4_0_OR_GREATER
using FLAGS = Godot.FileAccess.ModeFlags;
using ACCESS = Godot.FileAccess;
using Godot;
#else
using System.IO;
#endif

using System;

public partial class File : IDisposable
{
    public readonly string SystemPath;

#if GODOT4_0_OR_GREATER
    public readonly string GodotPath;
    private ACCESS Access { get; set; }
    public FLAGS Flags { get; private set; }
#endif

    #region Constructor

    public File(string path)
    {
#if GODOT4_0_OR_GREATER
        SystemPath = ProjectSettings.GlobalizePath(path.Trim());
        GodotPath = ProjectSettings.LocalizePath(SystemPath);
#else
        SystemPath = Path.GetFullPath(path.Trim());
#endif
    }

    public void Dispose()
    {
#if GODOT4_0_OR_GREATER
        Access?.Close();
        Access = null;
#endif
        GC.SuppressFinalize(this);
    }

    ~File() => Dispose();

    #endregion

    #region Raw Functions

#if GODOT4_0_OR_GREATER
    public ACCESS Open(FLAGS flags, bool reopen = true)
    {
        if (Access == null || reopen)
        {
            Access?.Close();

            if (flags.HasFlag(FLAGS.Write))
                _ = new Directory(SystemPath.TrimToDirectory());

            Access = ACCESS.Open(SystemPath, Flags = flags);
            if (Access.IsNull()) Debug.LogError($"FileOpen failed: {ACCESS.GetOpenError()}");
        }
        return Access;
    }

    public void Flush()
    {
        Access?.Flush();
    }

    public void Close()
    {
        Access?.Close();
        Access = null;
    }

    public byte[] ReadRaw(ulong length)
    {
        return Open(FLAGS.Read, false)?.GetBuffer((long)length) ?? [];
    }

    public void WriteRaw(byte[] buffer)
    {
        if (buffer?.Length > 0)
            Open(FLAGS.Write, false)?.StoreBuffer(buffer);
    }

    public ulong GetFileSizeRaw()
    {
        return Open(FLAGS.Read, false)?.GetLength() ?? 0;
    }
#else
    public byte[] ReadRaw(long length)
    {
        return System.IO.File.ReadAllBytes(SystemPath);
    }

    public void WriteRaw(byte[] buffer)
    {
        System.IO.File.WriteAllBytes(SystemPath, buffer);
    }

    public long GetFileSizeRaw()
    {
        return new FileInfo(SystemPath).Length;
    }

    public void Flush() { }

    public void Close() { }
#endif

    #endregion

    #region Base Functions

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
            Flush();
            Close();
        }
    }

    #endregion

    #region Encoder functions

    public bool TryRead<T>(out T _output)
    {
        return Read().TryDecode(out _output);
    }

    public T Read<T>()
    {
        return TryRead(out T _output) ? _output : default;
    }

    public void Write(object _input)
    {
        Write(_input.Encode());
    }

    #endregion

    #region String functions

    public void WriteString(string _string)
    {
        if (_string.IsEmpty()) return;

        using var _stream = new System.IO.MemoryStream();
        using var _writer = new System.IO.StreamWriter(_stream); // Because of plain text

        _writer.Write(_string);
        _writer.Flush();

        Write(_stream.ToArray());
    }

    public string ReadString()
    {
        using var _stream = new System.IO.MemoryStream(Read());
        using var _reader = new System.IO.StreamReader(_stream); // Because of plain text

        var _string = _reader.ReadToEnd();
        return _string.NotEmpty() ? _string : string.Empty;
    }

    public string[] ReadStringLines(bool _include_empty_lines = false)
    {
        return ReadString().Split(['\n', '\r'], _include_empty_lines ? StringSplitOptions.TrimEntries : CONST.StringSplit) ?? [];
    }

    public T ReadJson<T>() => ReadString().json<T>();

    #endregion

    #region GDResource functions

#if GODOT4_0_OR_GREATER
    public T ReadGDResource<T>() where T : Resource
    {
        return GD.Load(GodotPath) is T _t && _t.NotNull() ? _t : default;
    }
#endif

    #endregion

    #region File System

    public bool Exists()
    {
#if GODOT4_0_OR_GREATER
        return ACCESS.FileExists(SystemPath);
#else
            return File.Exists(SystemPath);
#endif
    }

    public void Delete()
    {
#if GODOT4_0_OR_GREATER
        DirAccess.RemoveAbsolute(SystemPath);
#else
            System.IO.File.Delete(SystemPath);
#endif
    }

    public Directory[] GetSiblingDirectories()
    {
        return new Directory(SystemPath).GetSubDirectories();
    }

    public File[] GetSiblingFiles()
    {
        var _siblings = new Directory(SystemPath).GetSubFiles();

        if (_siblings.Length > 1)
        {
            var _files = new File[_siblings.Length - 1];

            for (int i = 0, k = 0; i < _siblings.Length; i++, k++)
            {
                if (i == k && _siblings[i].SystemPath == SystemPath) i++;
                else _files[k] = _siblings[i];
            }

            return _files;
        }

        return [];
    }

    #endregion
}
