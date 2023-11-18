using System.Text.Json;
using System;

using Cutulu.JsonConverter;
using Godot;

using JsonEncoder = System.Text.Encodings.Web.JavaScriptEncoder;
using FileAccessG = Godot.FileAccess;

namespace Cutulu
{
	public static class IO
	{
		public const string PROJECT_PATH = "res://";
		public const string USER_PATH = "user://";

		#region JSON Utility
		#region Godot Extension
		public static T jsonG<T>(this string json) where T : Type => jsonG(json).Obj as T;
		public static Variant jsonG(this string json) => Json.ParseString(json);
		public static string jsonG(this Variant obj) => Json.Stringify(obj);
		#endregion

		#region Json Core
		private static JsonSerializerOptions options;
		private static bool currentFormat;

		public static JsonSerializerOptions JsonOptions(bool simpleFormat = true, bool indentFormat = false)
		{
			if (options == null)
			{
				options = new JsonSerializerOptions();

				// Register types
				options.Converters.Add(new Vector2I_Json());
				options.Converters.Add(new Vector3I_Json());

				// Force format update
				currentFormat = !simpleFormat;
			}

			if (options.WriteIndented != indentFormat)
				options.WriteIndented = indentFormat;

			// Update Format
			if (currentFormat != simpleFormat)
			{
				currentFormat = simpleFormat;
				options.Encoder = simpleFormat ? JsonEncoder.UnsafeRelaxedJsonEscaping : JsonEncoder.Default;
			}

			return options;
		}

		public static void RegisterJsonConverter(System.Text.Json.Serialization.JsonConverter converter)
		{
			JsonOptions(currentFormat).Converters.Add(converter);
		}
		#endregion

		#region From Json
		public static T json<T>(this string json) => json<T>(json, currentFormat);
		public static T json<T>(this string json, bool simpleFormat = true, bool indentFormat = false) => json<T>(json, default, simpleFormat, indentFormat);
		public static T json<T>(this string json, string encryptionKey = default, bool simpleFormat = true, bool indentFormat = false)
			=> json.IsEmpty() ? default : JsonSerializer.Deserialize<T>(encryptionKey.IsEmpty() ? json : json.DecryptString(encryptionKey), JsonOptions(simpleFormat, indentFormat)).wasJson();

		public static T jsonCurrentFormat<T>(this string json, string encryptionKey = default)
			=> json.IsEmpty() ? default : JsonSerializer.Deserialize<T>(encryptionKey.IsEmpty() ? json : json.DecryptString(encryptionKey), JsonOptions(currentFormat, options.WriteIndented)).wasJson();

		private static T wasJson<T>(this T t)
		{
			if (!t.Equals(default) && t is WasJson)
				(t as WasJson).OnReadFromJson();

			return t;
		}
		#endregion

		#region To Json
		public static string json(this object obj) => json(obj, "");
		public static string json(this object obj, bool simpleFormat = true, bool indentFormat = false) => json(obj, default, simpleFormat, indentFormat);
		public static string json(this object obj, string encryptionKey = default, bool simpleFormat = true, bool indentFormat = false)
			=> obj == null ? "" : encryptionKey.IsEmpty() ?
			JsonSerializer.Serialize(obj, JsonOptions(simpleFormat, indentFormat)) :
			JsonSerializer.Serialize(obj, JsonOptions(simpleFormat, indentFormat)).EncryptString(encryptionKey);

		public static string jsonCurrentFormat(this object obj, string encryptionKey = default)
			=> obj == null ? "" : encryptionKey.IsEmpty() ?
			JsonSerializer.Serialize(obj, JsonOptions(currentFormat, options.WriteIndented)) :
			JsonSerializer.Serialize(obj, JsonOptions(currentFormat, options.WriteIndented));
		#endregion
		#endregion

		#region File Managment
		public static Error mkDir(this string path) => DirAccess.MakeDirAbsolute(path.TrimToDirectory());

		public static void WriteText(this string path, string content, string encryptionKey = "", bool instantFlush = true)
		{
			if (path.IsEmpty()) "No path assigned!".Throw();
			
			mkDir(path = path.Trim());

			FileAccess file = FileAccessG.Open(path, FileAccessG.ModeFlags.Write);
			file.StoreString(encryptionKey.IsEmpty() ? content : content.EncryptString(encryptionKey));

			if (instantFlush) file.Flush();
		}

		public static string ReadText(this string path, string encryptionKey = "")
		{
			if (path.IsEmpty()) "No path assigned!".Throw();

			string content = FileAccessG.Open(path, FileAccessG.ModeFlags.Read).GetAsText();
			return encryptionKey.NotEmpty() ? content.DecryptString(encryptionKey) : content;
		}

		public static bool Exists(this string path) => FileAccessG.FileExists(path);

		public static void DeleteFile(this string path)
		{
			if (Exists(path)) DirAccess.RemoveAbsolute(path);
		}

		public static bool TryLoadJson<T>(this string path, out T asset)
		{
			if (TryLoadTxt(path, out string json))
			{
				asset = json.json<T>();

				return !asset.Equals(default(T));
			}

			asset = default;
			return false;
		}

        public static bool TryLoadTxt(this string path, out string text, string encyptionKey = null)
        {
            if (Exists(path = path.Trim()))
            {
                text = ReadText(path, encyptionKey);
				return true;
            }

            text = "";
            return false;
        }

        public static bool TryLoad<T>(this string path, out T asset) where T : class
		{
			path = path.Trim();

			if (Exists(path))
			{
				asset = GD.Load<T>(path);
				return asset != default(T);
			}

			asset = default;
			return false;
		}
		#endregion
	}

	public interface WasJson
	{
		public void OnReadFromJson();
	}
}