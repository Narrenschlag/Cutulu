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
        public const string LocalHost = "127.0.0.1";
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
        /// <summary>
        /// Don't forget the class/struct requires an empty constructor
        /// </summary>
        public static T json<T>(this string json) => json<T>(json, currentFormat);

        /// <summary>
        /// Don't forget the class/struct requires an empty constructor
        /// </summary>
        public static T json<T>(this string json, bool simpleFormat = true, bool indentFormat = false) => json<T>(json, default, simpleFormat, indentFormat);

        /// <summary>
        /// Don't forget the class/struct requires an empty constructor
        /// </summary>
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
                throw new($"json: {json}\n{error.Message}");
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
        /// Creates directory if not existing already.
        /// </summary>
        public static Error MkDir(this string path) => DirAccess.MakeDirAbsolute(path.TrimToDirectory());

        /// <summary>
        /// Writes text down in a file opened/created on the run.
        /// </summary>
        public static void WriteString(this string path, string content, string encryptionKey = null)
        {
            // Check if the path is valid and create dir if non existant
            if (path.IsEmpty()) throw new("No path assigned!");
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
        public static void WriteBytes(this string path, byte[] bytes)
        {
            // Check if buffer and path are valid and create dir if non existant
            if (bytes == null)
            {
                "No byte buffer assigned".LogError();
                return;
            }

            if (path.IsEmpty()) throw new("No path assigned!");

            MkDir(path = path.Trim());

            // Create/Open file
            FA file = FA.Open(path, FA.ModeFlags.Write);
            file.StoreBuffer(bytes);
            file.Flush();
            file.Close();
        }

        /// <summary>
        /// Reads text from file at given path.
        /// </summary>
        public static string ReadString(this string path, string decryptionKey = null)
        {
            // Check if the path is valid and create dir if non existant
            if (path.IsEmpty()) throw new("No path assigned!");
            if (!Exists(path)) throw new($"Path '{path}' does not exists.");

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
            if (path.IsEmpty()) throw new("No path assigned!");
            if (path.Exists() == false) throw new($"No file found at <{path}>.");

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
        => DA.DirExistsAbsolute(path.EndsWith('/') || path.EndsWith('\\') ? path : path += '/') ? DA.GetFilesAt(path) : null;

        /// <summary>
        /// Returns directories at given path
        /// </summary>
        public static string[] GetDirectories(this string path)
        => DA.DirExistsAbsolute(path.EndsWith('/') || path.EndsWith('\\') ? path : path += '/') ? DA.GetDirectoriesAt(path) : null;

        /// <summary>
        /// Defines the type serialization used for the file
        /// </summary>
        public enum FileType : byte
        {
            Json,
            Binary,
            GDResource
        }

        /// <summary>
        /// Returns file as defined type T
        /// </summary>
        public static T Read<T>(this string path, FileType type = FileType.Json) => TryRead(path, out T output, type) ? output : output;

        /// <summary>
        /// Returns file as defined type T
        /// </summary>
        public static bool TryRead<T>(this string path, out T output, FileType type = FileType.Json)
        {
            if (path.IsEmpty())
            {
                output = default;
                return false;
            }

            // Check if file exists
            if (Exists(path = path.Trim()))
            {
                // Check file type
                switch (type)
                {
                    // Handle binary reading
                    case FileType.Binary:
                        return path.ReadBytes().TryDecode(out output);

                    // Handle json reading
                    case FileType.Json:
                        // Try reading as json
                        try
                        {
                            output = path.ReadString().json<T>();
                            return true;
                        }

                        // Something failed
                        catch
                        {
                            break;
                        }

                    // Handle godot resource loading
                    case FileType.GDResource:
                        // Validate T is a class
                        if (typeof(T).IsClass)
                        {
                            var resource = GD.Load(path);
                            if (resource.NotNull())
                            {
                                output = (T)(object)resource;
                                return true;
                            }
                        }

                        break;

                    // Edge case
                    default:
                        break;
                }
            }

            output = default;
            return false;
        }

        /// <summary>
        /// Writes instance of type T as file
        /// </summary>
        public static bool Write<T>(this T input, string path, FileType type = FileType.Json, bool overwrite = true)
        {
            // Validate input
            if (input == null) return false;

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
                    WriteBytes(path, input.Encode());
                    return true;

                // Handle json writing
                case FileType.Json:
                    WriteString(path, input.json());
                    return true;

                // Handle godot resource loading
                case FileType.GDResource:
                    // Validate that input is Resource
                    if (input is Resource)
                    {
                        // Save the resource to the file
                        ResourceSaver.Save(input as Resource, path);
                        return true;
                    }

                    return false;

                // Edge case
                default:
                    return false;
            }
        }
        #endregion

        public static void Append(this ZipPacker packer, string path, byte[] buffer)
        {
            packer.StartFile(path);
            packer.WriteFile(buffer);
            packer.CloseFile();
        }
    }

    public interface IWasJson
    {
        public void OnReadFromJson();
    }
}