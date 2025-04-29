using System.Collections.Generic;
using System.Text;
using Godot;

namespace Cutulu.Core
{
    /// <summary>
    /// OE - Odin's Eye is used to find and read files that may be nested in zip files. Important for mod support.
    /// Using '?' as a directory splitter '/' for (nested) zip files.
    /// </summary>
    public static class OE
    {
        #region Find Data
        /// <summary>
        /// Adds found file paths to the references list. Crawling directories and zip files if assigned endings.
        /// </summary>
        public static void FindFiles(string rootFolder, ref List<string> filePaths, string[] fileEndings, string[] zipFileEndings = null)
        {
            if (rootFolder.EndsWith('/') == false) rootFolder += '/';

            var files = IO.GetFileNames(rootFolder);
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

                    if (zipFileEndings.NotEmpty())
                    {
                        for (int z = 0; z < zipFileEndings.Length; z++)
                        {
                            CrawlZip($"{rootFolder}{files[i]}", ref filePaths, fileEndings, zipFileEndings);
                        }
                    }
                }
            }

            var directories = IO.GetDirectoryNames(rootFolder);
            if (directories.NotEmpty())
            {
                for (int i = 0; i < directories.Length; i++)
                {
                    FindFiles($"{rootFolder}{directories[i]}/", ref filePaths, fileEndings, zipFileEndings);
                }
            }
        }

        private static void CrawlZip(string zipFilePath, ref List<string> filePaths, string[] fileEndings, string[] zipFileEndings)
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
                            var _buffer = reader.ReadFile(files[i]);
                            CrawZipInternal($"{zipFilePath}?{files[i]}", _buffer, ref filePaths);
                        }
                    }
                }
            }

            reader.Close();

            void CrawZipInternal(string prePath, byte[] buffer, ref List<string> filePaths)
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
                            var _buffer = reader.ReadFile(files[i]);
                            CrawZipInternal($"{prePath}?{files[i]}", _buffer, ref filePaths);
                        }
                    }
                }

                reader.Close();
            }
        }
        #endregion

        #region Read Data
        /// <summary>
        /// Returns data from path as given file type if existing and parsable.
        /// </summary>
        public static bool TryGetData<T>(string path, out T result, IO.FileType type = IO.FileType.Binary) where T : class
        {
            if (TryGetData(path, out var buffer) && buffer.NotEmpty())
            {
                switch (type)
                {
                    case IO.FileType.Binary:
                        Debug.Log($"buffer[{buffer.Length}] to {typeof(T).Name}");
                        return buffer.TryDecode(out result);

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

                            // Support for OGG files
                            if (t == typeof(AudioStream)) result = (T)(object)AudioStreamOggVorbis.LoadFromBuffer(buffer);

                            else
                            {
                                // Write temp zip file
                                var temp = $"{IO.USER_PATH}.bin/.temp/";
                                DirAccess.MakeDirRecursiveAbsolute(temp);
                                (temp += $"temp_buffer.{path[path.TrimEndUntil('.').Length..]}").WriteBytes(buffer);

                                // Support for Texture2D
                                if (t == typeof(Texture2D))
                                {
                                    var img = new Image();
                                    img.Load(temp);

                                    result = (T)(object)ImageTexture.CreateFromImage(img);
                                }

                                // Load resource from temp file
                                else result = (T)(object)ResourceLoader.Load(temp, t.Name, ResourceLoader.CacheMode.Reuse);

                                temp.DeleteFile();


                            }

                            if (result != null)
                            {
                                (result as Resource).ResourcePath = $"$temp/{path}";
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

            var paths = path.Split('?', CONST.StringSplit);

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
                        buffer = reader.ReadFile(paths[i]);
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

        /// <summary>
        /// Returns if file is existing in either a directory or a (nested) zip file.
        /// </summary>
        public static bool Exists(string path) => TryGetData(path, out var buffer) && buffer.NotEmpty();
        #endregion
    }
}