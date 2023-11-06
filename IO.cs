using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.IO;
using System;

using Cutulu.JsonConverter;
using Godot;

using JsonEncoder = System.Text.Encodings.Web.JavaScriptEncoder;
using FileAccessG = Godot.FileAccess;
using System.Threading;

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
		public static T json<T>(this string json) => json<T>(json, "");
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

		#region File Base
		public static FileAccessG OpenFile(this string path, FileAccessG.ModeFlags mode = FileAccessG.ModeFlags.WriteRead) => FileAccessG.Open(path, mode);
		public static FileAccessG CreateFile(this string path) => OpenFile(path, FileAccessG.ModeFlags.WriteRead);

		public static Error mkDir(this string path) => DirAccess.MakeDirAbsolute(path.TrimToDirectory());

		public static void SaveString(this string path, string content, string encryptionKey = "")
		{
			if (string.IsNullOrEmpty(path)) throw new NullReferenceException("No path assigned!");
			mkDir(path);

			using var file = FileAccessG.Open(path.Trim(), FileAccessG.ModeFlags.Write);
			file.StoreString(string.IsNullOrEmpty(encryptionKey) ? content : content.EncryptString(encryptionKey));
		}

		public static string LoadString(this string path, string encryptionKey = "")
		{
			if (string.IsNullOrEmpty(path)) throw new NullReferenceException("No path assigned!");

			using FileAccessG file = FileAccessG.Open(path, FileAccessG.ModeFlags.Read);
			string content = content = file.GetAsText();

			if (!string.IsNullOrEmpty(encryptionKey))
				content = content.DecryptString(encryptionKey);

			return content;
		}

		public static bool Exists(this string path) => FileAccessG.FileExists(path);

		public static void DeleteFile(this string path) => ClearFile(path);
		public static void ClearFile(this string path)
		{
			if (Exists(path)) DirAccess.RemoveAbsolute(path);
		}
		#endregion

		#region Encryption
		public static string EncryptString(this string plaintext, string encryption_key)
		{
			// Convert the plaintext string to a byte array
			byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

			// Derive a new password using the PBKDF2 algorithm and a random salt
			Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(encryption_key, 20);

			// Use the password to encrypt the plaintext
			Aes encryptor = Aes.Create();

			encryptor.Key = passwordBytes.GetBytes(32);
			encryptor.IV = passwordBytes.GetBytes(16);

			using (MemoryStream ms = new MemoryStream())
			{
				using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
				{
					cs.Write(plaintextBytes, 0, plaintextBytes.Length);
				}

				return Convert.ToBase64String(ms.ToArray());
			}
		}

		public static string DecryptString(this string encrypted, string encryption_key)
		{
			// Convert the encrypted string to a byte array
			byte[] encryptedBytes = Convert.FromBase64String(encrypted);

			// Derive the password using the PBKDF2 algorithm
			Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(encryption_key, 20);

			// Use the password to decrypt the encrypted string
			Aes encryptor = Aes.Create();

			encryptor.Key = passwordBytes.GetBytes(32);
			encryptor.IV = passwordBytes.GetBytes(16);

			using (MemoryStream ms = new MemoryStream())
			{
				using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
				{
					cs.Write(encryptedBytes, 0, encryptedBytes.Length);
				}

				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}

		#region Obsolete
		/*
		public static string EncryptString(this string plainText, string key)
		{
			using Aes aesAlg = Aes.Create();
			aesAlg.Key = Encoding.UTF8.GetBytes(key);

			ICryptoTransform encryptor = aesAlg.CreateEncryptor();

			using MemoryStream msEncrypt = new MemoryStream();
			using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
			using StreamWriter swEncrypt = new StreamWriter(csEncrypt);

			swEncrypt.Write(plainText);

			return Convert.ToBase64String(msEncrypt.ToArray());
		}

		public static string DecryptString(this string cipherText, string key)
		{
			using Aes aesAlg = Aes.Create();
			aesAlg.Key = Encoding.UTF8.GetBytes(key);

			ICryptoTransform decryptor = aesAlg.CreateDecryptor();

			using MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
			using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
			using StreamReader srDecrypt = new StreamReader(csDecrypt);

			return srDecrypt.ReadToEnd();
		}

		public static string GenerateAESKey(int keySizeInBytes = 32) => Convert.ToBase64String(GenerateAESKeyBytes(keySizeInBytes));
		public static byte[] GenerateAESKeyBytes(int keySizeInBytes = 32)
		{
			using (RndGenSys rng = RndGenSys.Create())
			{
				byte[] key = new byte[keySizeInBytes];
				rng.GetBytes(key);
				return key;
			}
		}
		*/
		#endregion
		#endregion

		#region Mod Support
		public static bool FindFile(this string localPath, out string fullPath, bool preferProjectFile = true)
		{
			fullPath = null;

			if (preferProjectFile)
			{
				if (Exists($"{PROJECT_PATH}{localPath}"))
					fullPath = PROJECT_PATH + localPath;

				else if (Exists($"{USER_PATH}{localPath}"))
					fullPath = USER_PATH + localPath;
			}
			else
			{
				if (Exists($"{USER_PATH}{localPath}"))
					fullPath = USER_PATH + localPath;

				else if (Exists($"{PROJECT_PATH}{localPath}"))
					fullPath = PROJECT_PATH + localPath;
			}

			return fullPath.NotEmpty();
		}

		public static bool TryLoad<T>(this string localPath, out T value, bool preferProjectFile = true) where T : class
		{
			if (FindFile(localPath, out string fullPath, preferProjectFile))
			{
				value = GD.Load<T>(fullPath);
				return value != default(T);
			}

			value = default(T);
			return false;
		}
		#endregion
	}

	public interface WasJson
	{
		public void OnReadFromJson();
	}
}