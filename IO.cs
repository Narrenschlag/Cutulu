using JsonEncoder = System.Text.Encodings.Web.JavaScriptEncoder;
using FA = Godot.FileAccess;
using DA = Godot.DirAccess;

using System.Text.Json;
using System;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Handles Input and Output of file streams to read and write files to the OS file system
    /// </summary>
    public static class IO
    {
        public const string PROJECT_PATH = "res://";
        public const string USER_PATH = "user://";

        #region JSON Utility    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static Dictionary<bool, bool, JsonSerializerOptions> options;
        private static bool currentFormat, currentIndent;

        public static JsonSerializerOptions JsonOptions(bool simpleFormat = true, bool indentFormat = false)
        {
            if (options.IsNull()) options = new Dictionary<bool, bool, JsonSerializerOptions>();
            if (!options.TryGetValue(simpleFormat, indentFormat, out var _options))
            {
                _options = new JsonSerializerOptions()
                {
                    Encoder = simpleFormat ? JsonEncoder.UnsafeRelaxedJsonEscaping : JsonEncoder.Default,
                    WriteIndented = indentFormat
                };

                options.Add(simpleFormat, indentFormat, _options);
            }

            currentFormat = simpleFormat;
            currentIndent = indentFormat;

            return _options;
        }

        public static void RegisterJsonConverter(System.Text.Json.Serialization.JsonConverter converter)
        {
            JsonOptions(currentFormat).Converters.Add(converter);
        }

        #region From Json       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static T json<T>(this string json) => json<T>(json, currentFormat);
        public static T json<T>(this string json, bool simpleFormat = true, bool indentFormat = false) => json<T>(json, default, simpleFormat, indentFormat);
        public static T json<T>(this string json, string decryptionKey = null, bool simpleFormat = true, bool indentFormat = false)
        {
            // No json
            if (json.IsEmpty()) return default;

            // Decryption
            if (decryptionKey.NotEmpty())
                json = json.DecryptString(decryptionKey);

            // Read T from json
            T t;
            try
            {
                t = JsonSerializer.Deserialize<T>(json, JsonOptions(simpleFormat, indentFormat));
            }
            catch (Exception error)
            {
                $"json: {json}\n{error.Message}".Throw();
                return default;
            }

            // Update custom json setup
            if (t is IWasJson) (t as IWasJson).OnReadFromJson();

            return t;
        }

        public static T jsonCurrentFormat<T>(this string json, string decryptionKey = null)
        => json<T>(json, decryptionKey, currentFormat, currentIndent);
        #endregion

        #region To Json         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static string json(this object obj) => json(obj, "");
        public static string json(this object obj, bool simpleFormat = true, bool indentFormat = false) => json(obj, default, simpleFormat, indentFormat);
        public static string json(this object obj, string encryptionKey = null, bool simpleFormat = true, bool indentFormat = false)
        {
            if (obj == null) return null;

            string json = JsonSerializer.Serialize(obj, JsonOptions(simpleFormat, indentFormat));

            if (encryptionKey.NotEmpty())
                json.EncryptString(encryptionKey);

            return json;
        }

        public static string jsonCurrentFormat(this object obj, string encryptionKey = null)
        => json(obj, encryptionKey, currentFormat, currentIndent);
        #endregion
        #endregion

        #region File Managment  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Load json from path. Throws errors if something fails.
        /// Recommended usage: try, catch
        /// </summary>
        public static T LoadJson<T>(this string path, string decryptionKey = null)
        {
            string json = Read(path, decryptionKey);
            return json.json<T>();
        }

        /// <summary>
        /// Try load class from path.
        /// </summary>
        public static bool TryLoad<T>(this string path, out T asset) where T : class
        {
            if (Exists(path = path.Trim()))
            {
                asset = GD.Load<T>(path);
                return true;
            }

            asset = default;
            return false;
        }

        /// <summary>
        /// Creates directory if not existing already.
        /// </summary>
        public static Error MkDir(this string path) => DirAccess.MakeDirAbsolute(path.TrimToDirectory());

        /// <summary>
        /// Writes text down in a file opened/created on the run.
        /// </summary>
        public static void Write(this string path, string content, string encryptionKey = null)
        {
            // Check if the path is valid and create dir if non existant
            if (path.IsEmpty()) "No path assigned!".Throw();
            MkDir(path = path.Trim());

            // Encrypt content
            if (encryptionKey.NotEmpty())
                content = content.EncryptString(encryptionKey);

            // Create/Open file
            FA file = FA.Open(path, FA.ModeFlags.Write);
            file.StoreString(content);
            file.Flush();
        }

        /// <summary>
        /// Write bytes into file at given path.
        /// </summary>
        public static void Write(this string path, byte[] bytes)
        {
            // Check if buffer and path are valid and create dir if non existant
            if (bytes == null)
            {
                "No byte buffer assigned".LogError();
                return;
            }

            if (path.IsEmpty()) "No path assigned!".Throw();

            MkDir(path = path.Trim());

            // Create/Open file
            FA file = FA.Open(path, FA.ModeFlags.Write);
            file.StoreBuffer(bytes);
            file.Flush();
        }

        /// <summary>
        /// Reads text from file at given path.
        /// </summary>
        public static string Read(this string path, string decryptionKey = null)
        {
            // Check if the path is valid and create dir if non existant
            if (path.IsEmpty()) "No path assigned!".Throw();
            if (!Exists(path)) $"Path '{path}' does not exists.".Throw();

            // Open file
            FA file = FA.Open(path, FA.ModeFlags.Read);
            string content = file.GetAsText();

            // Decrypt content
            if (decryptionKey.NotEmpty())
                content = content.DecryptString(decryptionKey);

            return content;
        }

        /// <summary>
        /// Read bytes from file at given path.
        /// </summary>
        public static byte[] ReadBytes(this string path)
        {
            // Check if the path is valid and create dir if non existant
            if (path.IsEmpty()) "No path assigned!".Throw();
            if (path.Exists() == false) $"No file found at <{path}>.".Throw();

            FA file = FA.Open(path, FA.ModeFlags.Read);
            return file.GetBuffer((long)file.GetLength());
        }

        /// <summary>
        /// Check if file exists at given path
        /// </summary>
        public static bool Exists(this string path) => FA.FileExists(path);

        /// <summary>
        /// Deletes file at given path
        /// </summary>
        public static void DeleteFile(this string path)
        {
            if (Exists(path)) DA.RemoveAbsolute(path);
        }

        /// <summary>
        /// Returns files at given path
        /// </summary>
        public static string[] GetFiles(this string path)
        {
            bool exists = DA.DirExistsAbsolute(path.EndsWith('/') || path.EndsWith('\\') ? path : path += '/');
            if (exists)
            {
                return DA.GetFilesAt(path);
            }

            return null;
        }

        /// <summary>
        /// Returns directories at given path
        /// </summary>
        public static string[] GetDirectories(this string path)
        {
            bool exists = DA.DirExistsAbsolute(path.EndsWith('/') || path.EndsWith('\\') ? path : path += '/');
            if (exists)
            {
                return DA.GetDirectoriesAt(path);
            }

            return null;
        }

        public enum FileType
        {
            Json, Binary
        }

        public static bool TryRead<T>(this string path, out T output, FileType type = FileType.Json)
        {
            // Check if file exists
            if (Exists(path))
            {
                // Check file type
                switch (type)
                {
                    // Handle binary reading
                    case FileType.Binary:
                        return path.ReadBytes().TryBuffer(out output);

                    // Handle json reading
                    case FileType.Json:
                        // Try reading as json
                        try
                        {
                            output = path.Read().json<T>();
                            return true;
                        }

                        // Something failed
                        catch
                        {
                            break;
                        }

                    // Edge case
                    default:
                        break;
                }
            }

            output = default;
            return false;
        }

        public static bool TryWrite<T>(this T input, string path, FileType type = FileType.Json, bool overwrite = true)
        {
            // Check for existing file and overwrite check
            if (overwrite == false && Exists(path))
            {
                return false;
            }

            // Check the file type
            switch (type)
            {
                // Handle binary writing
                case FileType.Binary:
                    Write(path, input.Buffer());
                    return true;

                // Handle json writing
                case FileType.Json:
                    Write(path, input.json());
                    return true;

                // Edge case
                default:
                    return false;
            }
        }
        #endregion
    }

    public interface IWasJson
    {
        public void OnReadFromJson();
    }
}