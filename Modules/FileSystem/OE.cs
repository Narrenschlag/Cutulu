using System.Collections.Generic;
using System.Text;
using Godot;

namespace Cutulu.Modding
{
    /// <summary>
    /// OE - Odin's Eye is used to find and read files that may be nested in zip files. Important for mod support.
    /// </summary>
    public static class OE
    {
        #region Find Data
        public static void FindFiles(string rootFolder, ref List<string> filePaths, string[] fileEndings, string[] zipFileEndings = null)
        {
            if (rootFolder.EndsWith('/') == false) rootFolder += '/';

            var files = IO.GetFiles(rootFolder);
            if (files.NotEmpty())
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var added = false;

                    for (int e = 0; e < fileEndings.Length; e++)
                    {
                        if (files[i].EndsWith(fileEndings[e]))
                        {
                            filePaths.Add($"{rootFolder}{files[i]}");
                            added = true;
                            break;
                        }
                    }

                    if (added) continue;

                    for (int z = 0; z < zipFileEndings.Length; z++)
                    {
                        FindFilesInZip($"{rootFolder}{files[i]}", ref filePaths, fileEndings, zipFileEndings);
                    }
                }
            }

            var directories = IO.GetDirectories(rootFolder);
            if (directories.NotEmpty())
            {
                for (int i = 0; i < directories.Length; i++)
                {
                    FindFiles($"{rootFolder}{directories[i]}/", ref filePaths, fileEndings, zipFileEndings);
                }
            }
        }

        public static void FindFilesInZip(string zipFilePath, ref List<string> filePaths, string[] fileEndings, string[] zipFileEndings)
        {
            if (zipFileEndings.IsEmpty() || fileEndings.IsEmpty()) return;
            var reader = new ZipReader();

            if (reader.Open(zipFilePath) != Error.Ok) return;

            var files = reader.GetFiles();

            if (files.NotEmpty())
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var added = false;

                    for (int e = 0; e < fileEndings.Length; e++)
                    {
                        if (files[i].EndsWith(fileEndings[e]))
                        {
                            filePaths.Add($"{zipFilePath}?{files[i]}");
                            added = true;
                            break;
                        }
                    }

                    if (added) continue;

                    for (int z = 0; z < zipFileEndings.Length; z++)
                    {
                        if (files[i].EndsWith(zipFileEndings[z]))
                        {
                            var _buffer = reader.ReadFile(files[i], false);
                            searchInternal($"{zipFilePath}?{files[i]}", _buffer, ref filePaths);
                        }
                    }
                }
            }

            reader.Close();

            void searchInternal(string prePath, byte[] buffer, ref List<string> filePaths)
            {
                if (buffer.IsEmpty()) return;

                var temp = $"{IO.USER_PATH}.bin/.temp/";
                DirAccess.MakeDirRecursiveAbsolute(temp);
                (temp += "zip.temp").WriteBytes(buffer);

                var reader = new ZipReader();
                if (reader.Open(temp) != Error.Ok) return;

                var files = reader.GetFiles();
                if (files.IsEmpty())
                {
                    reader.Close();
                    return;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    var added = false;

                    for (int e = 0; e < fileEndings.Length; e++)
                    {
                        if (files[i].EndsWith(fileEndings[e]))
                        {
                            filePaths.Add($"{prePath}?{files[i]}");
                            added = true;
                            break;
                        }
                    }

                    if (added) continue;

                    for (int z = 0; z < zipFileEndings.Length; z++)
                    {
                        if (files[i].EndsWith(zipFileEndings[z]))
                        {
                            var _buffer = reader.ReadFile(files[i], false);
                            searchInternal($"{prePath}?{files[i]}", _buffer, ref filePaths);
                        }
                    }
                }

                reader.Close();
            }
        }
        #endregion

        #region Read Data
        public static bool TryGetData<T>(string path, out T result, IO.FileType type = IO.FileType.Binary)
        {
            if (TryGetData(path, out var buffer) && buffer.NotEmpty())
            {
                switch (type)
                {
                    case IO.FileType.Binary:
                        Debug.Log($"buffer[{buffer.Length}] to {typeof(T).Name}");
                        return buffer.TryBuffer(out result);

                    case IO.FileType.Json:
                        try
                        {
                            result = Encoding.UTF8.GetString(buffer).json<T>();
                            return true;
                        }

                        catch
                        {
                            break;
                        }

                    case IO.FileType.GDResource:
                        try
                        {
                            var t = typeof(T);

                            // Support for Models
                            if (t == typeof(GlbModel)) result = (T)(object)GlbModel.CustomImport(buffer);

                            // Support for OGG files
                            else if (t == typeof(AudioStream)) result = (T)(object)AudioStreamOggVorbis.LoadFromBuffer(buffer);

                            else
                            {
                                // Write temp zip file
                                var temp = $"{IO.USER_PATH}.bin/.temp/";
                                DirAccess.MakeDirRecursiveAbsolute(temp);
                                (temp += $"buffer.{path[path.TrimEndUntil('.').Length..]}").WriteBytes(buffer);

                                // Support for Texture2D
                                if (t == typeof(Texture2D))
                                {
                                    var img = new Image();
                                    img.Load(temp);

                                    result = (T)(object)ImageTexture.CreateFromImage(img);
                                }

                                // Load resource from temp file
                                else result = (T)(object)ResourceLoader.Load(temp, t.FullName);

                                temp.DeleteFile();

                                result = (T)(object)ResourceLoader.Load(temp, typeof(T).Name);
                            }

                            return result != null;
                        }

                        catch (System.Exception ex)
                        {
                            Debug.LogError($"Cannot load {typeof(T).Name}\n{ex.Message}");
                            break;
                        }

                    default:
                        break;
                }

                result = default;
                return false;
            }

            result = default;
            return false;
        }

        public static bool TryGetData(string path, out byte[] buffer)
        {
            buffer = null;

            if (path.IsEmpty()) return false;

            var paths = path.Split('?', Core.StringSplit);

            if (paths.IsEmpty() || paths[0].Exists() == false) return false;

            // Is a zip file entry
            if (paths.Length > 1)
            {
                var reader = new ZipReader();

                for (int i = 1; i < paths.Length; i++)
                {
                    // Assign net path as zip file to open
                    if (i <= 1)
                    {
                        // Cant open zip file
                        if (reader.Open(paths[0]) != Error.Ok)
                        {
                            buffer = null;
                            break;
                        }
                    }

                    // Create temp file for zip file to open
                    else
                    {
                        // Write temp zip file
                        var temp = $"{IO.USER_PATH}.bin/.temp/";
                        DirAccess.MakeDirRecursiveAbsolute(temp);
                        (temp += "zip.temp").WriteBytes(buffer);

                        // Cant open zip file
                        if (reader.Open(temp) != Error.Ok)
                        {
                            buffer = null;
                            break;
                        }
                    }

                    // Try find zip file
                    if (reader.FileExists(paths[i]))
                    {
                        buffer = reader.ReadFile(paths[i], false);
                    }

                    // Cant find file
                    else
                    {
                        buffer = null;
                        break;
                    }
                }

                // Close reader
                reader.Close();
            }

            // Is a net file
            else
            {
                buffer = paths[0].ReadBytes();
            }

            // Return true if buffer could be read
            return buffer.NotEmpty();
        }

        public static bool Exists(string path) => TryGetData(path, out var buffer) && buffer.NotEmpty();
        #endregion
    }
}