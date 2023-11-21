using System.Collections.Generic;
using System;

namespace Cutulu
{
	public static class SaveData
	{
		private static string CurrentSavePath;
		private static SaveCache _cache;

		public const string FILE_ENDING = "cutulu";

		#region Cross References
		public static void SimpleSave<T>(this string path, T content, string encryption_key = null) => path.Write(content.jsonCurrentFormat(), encryption_key);
		public static void SimpleSave(this string path, string content, string encryption_key = null) => path.Write(content, encryption_key);

		public static T SimpleLoad<T>(this string path, string decryption_key = null) => path.Read(decryption_key).jsonCurrentFormat<T>();
		public static string SimpleLoad(this string path, string decryption_key = null) => path.Read(decryption_key);
		#endregion

		#region Help functions
		private static string fixPath(string path) => string.IsNullOrEmpty(path) || path.EndsWith($".{FILE_ENDING}") ? path : path + $".{FILE_ENDING}";

		private static void error()
		{
			if (_cache.Equals(default))
				throw new NullReferenceException("You have to setup your SaveData first with SaveData.CreateNew(path, name) or SaveData.LoadExisting(path).");
		}

		public static void Value<T>(this string id, out T value, T defaultValue)
		{
			if (TryGet(id, out value)) return;

			value = defaultValue;
			Save(id, value);
		}
		#endregion

		#region File Handling
		public static void LoadOrCreateNew(string path, string name, string encryptionKey = "")
		{
			if (IO.Exists(fixPath(path))) LoadExisting(path, encryptionKey);
			else CreateNew(path, name);
		}

		public static void CreateNew(string path, string name)
		{
			_cache = new SaveCache(fixPath(path), name);
			CurrentSavePath = _cache.Path;

			$"New SaveFile at [{path}] has been created".Log();
		}

		public static void ReloadCurrent(string encryption_key = "") => LoadExisting(CurrentSavePath, encryption_key);
		public static void LoadExisting(string path, string encryption_key = "")
		{
			if (string.IsNullOrEmpty(path)) throw new NullReferenceException("No path assigned!");

			_cache = SaveCache.Load(fixPath(path), encryption_key);
			_cache.Path = path;

			CurrentSavePath = path;
			$"Existing SaveFile at [{path}] has been loaded from disc".Log();
		}

		public static void SaveToFile(string encryption_key = "")
		{
			error();

			if (string.IsNullOrEmpty(encryption_key)) _cache.WriteToFile();
			else _cache.WriteToFile(encryption_key);
		}

		public static void DeleteFile()
        {
            error();

			IO.DeleteFile(CurrentSavePath);
        }

        public static bool Exists(string path) => IO.Exists(path);
		#endregion

		#region Utility
		public static void Save(string key, object obj) => Save(obj, key);
		public static void Save(this object obj, string key)
		{
			error();

			_cache.Save(obj, key);
		}

		public static void Delete(this string key)
		{
			error();

			_cache.Delete(key);
		}

		public static bool Contains(this string key)
		{
			error();

			return _cache.Contains(key.Trim());
		}

		public static bool TryGet<T>(this string key, out T value)
		{
			error();

			return _cache.TryGet(key, out value);
		}
		#endregion

		#region Cache Backend
		// Cache used for storage
		private struct SaveCache
		{
			public string Name { get; set; }
			public string Path { get; set; }

			public Dictionary<string, string> RAMJ { get; set; }
			public Dictionary<string, object> RAM;

			// Create new save cache
			public SaveCache(string path, string name)
			{
				RAMJ = new Dictionary<string, string>();
				RAM = new Dictionary<string, object>();

				Path = path;
				Name = name;
			}

			public SaveCache()
			{
				RAMJ = new Dictionary<string, string>();
				RAM = new Dictionary<string, object>();

				Path = "";
				Name = "";
			}

			// Load existing save cache
			public static SaveCache Load(string path, string decryption_key = "")
			{
				if (!IO.Exists(path)) return new SaveCache();

				return path.Read(decryption_key).json<SaveCache>();
			}

			public void WriteToFile(string encryption_key = "")
			{
				Dictionary<string, string> RAMJtemp = new Dictionary<string, string>();

				// Add updated
				foreach (KeyValuePair<string, object> entry in RAM)
                    RAMJtemp.Add(entry.Key, entry.Value.json());

				// Add old entries
				foreach (string key in RAMJ.Keys)
					if (!RAMJtemp.ContainsKey(key)) RAMJtemp.Add(key, RAMJ[key]);

				RAMJ = RAMJtemp;
				Path.Write(this.json(), encryption_key);
			}

			public bool TrySave(object obj, string key)
			{
				key = key.Trim();

				if (RAM.ContainsKey(key)) return false;
				else RAM.Add(key, obj);

				return true;
			}

			public void Save(object obj, string key)
			{
				key = key.Trim();

				if (RAM.ContainsKey(key)) RAM[key] = obj;
				else RAM.Add(key, obj);
			}

			public void Delete(string key)
			{
				key = key.Trim();

				if (RAMJ.ContainsKey(key)) RAMJ.Remove(key);
				if (RAM.ContainsKey(key)) RAM.Remove(key);
			}

			public bool Contains(string key) => RAMJ.ContainsKey(key.Trim());
			public bool TryGet<T>(string key, out T value)
			{
				//$"[SaveData:{key}] : ({RAM.Count}/{RAMJ.Count})".Log();
				key = key.Trim();

				if (RAM.TryGetValue(key, out object obj))
				{
					value = (T)obj;
					return true;
				}

				else if (RAMJ.TryGetValue(key, out string json))
				{
					value = json.json<T>();
					RAM.Add(key, value);
					return true;
				}

				value = default(T);
				return false;
			}
		}
		#endregion
	}
}
