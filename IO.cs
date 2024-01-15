using JsonEncoder = System.Text.Encodings.Web.JavaScriptEncoder;
using FA = Godot.FileAccess;
using DA = Godot.DirAccess;

using System.Text.Json;
using System;
using Godot;

namespace Cutulu
{
    public static class IO
    {
        public const string PROJECT_PATH = "res://";
        public const string USER_PATH = "user://";

        #region JSON Utility
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

        #region From Json
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
            if (t is WasJson) (t as WasJson).OnReadFromJson();

            return t;
        }

        public static T jsonCurrentFormat<T>(this string json, string decryptionKey = null)
        => json<T>(json, decryptionKey, currentFormat, currentIndent);
        #endregion

        #region To Json
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

        #region File Managment
        /// <summary>
        /// Load json from path. Throws errors if something fails.
        /// Recommended usage: try,catch
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
        /// Creates directory if not existant already.
        /// </summary>
        public static Error mkDir(this string path) => DirAccess.MakeDirAbsolute(path.TrimToDirectory());

        /// <summary>
        /// Writes text down in a file opened/created on the run.
        /// </summary>
        public static void Write(this string path, string content, string encryptionKey = null, bool instantFlush = true)
        {
            // Check if the path is valid and create dir if non existant
            if (path.IsEmpty()) "No path assigned!".Throw();
            mkDir(path = path.Trim());

            // Encrypt content
            if (encryptionKey.NotEmpty())
                content = content.EncryptString(encryptionKey);

            // Create/Open file
            FA file = FA.Open(path, FA.ModeFlags.Write);
            file.StoreString(content);

            // Flush and thereby finally write file to storage
            if (instantFlush) file.Flush();
        }

        /// <summary>
        /// Writes text down in a file opened/created on the run.
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
        /// Check if file exists
        /// </summary>
        public static bool Exists(this string path) => FA.FileExists(path);

        /// <summary>
        /// Ereases file from directory
        /// </summary>
        public static void DeleteFile(this string path)
        {
            if (Exists(path)) DA.RemoveAbsolute(path);
        }
        #endregion
    }

    public interface WasJson
    {
        public void OnReadFromJson();
    }
}