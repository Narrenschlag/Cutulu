namespace Cutulu.Core
{
    using JsonEncoder = System.Text.Encodings.Web.JavaScriptEncoder;

    using System.Text.Json;
    using System;
    using Godot;

    /// <summary>
    /// Handles Input and Output of file streams to read and write files to the OS file system
    /// </summary>
    public static class IO
    {
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