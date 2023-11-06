using System.Collections.Generic;
using Godot;

namespace Cutulu
{
	public partial class GlobalValues : Node
	{
		private static Dictionary<string, object> values;
		public static Dictionary<string, object> Values
		{
			get
			{
				if (values == null) values = new Dictionary<string, object>();
				return values;
			}
		}

		public static void Set<T>(string key, T value) { if (!TryAdd(key, value)) Values[key.Trim()] = value; }
		public static bool TryAdd<T>(string key, T value)
		{
			if (HasValue(key)) return false;

			Values.Add(key.Trim(), value);
			return true;
		}

		public static T Get<T>(string key, T @default) => TryGet(key.Trim(), out T result) ? result : @default;
		public static bool TryGet<T>(string key, out T value)
		{
			if (Values.TryGetValue(key, out object result))
			{
				value = (T)result;
				return true;
			}

			value = default;
			return false;
		}

		public static void Remove(string key) { if (HasValue(key)) Values.Remove(key.Trim()); }

		public static bool HasValue(string key) => Values.ContainsKey(key.Trim());

		public void try_add_value(string key, object value) => TryAdd(key, value);
		public object get_value(string key) => Get(key.Trim(), default(object));
		public void set_value(string key, object value) => Set(key, value);
		public void remove_value(string key) => Remove(key.Trim());
		public bool has_value(string key) => HasValue(key);

		#region String
		public void set_string(string key, string value) => Set(key, value);
		public string get_string(string key, string @default) => Get(key, @default);
		public string get_string(string key) => Get(key, default(string));
		#endregion

		#region Integer
		public void set_int(string key, int value) => Set(key, value);
		public int get_int(string key, int @default) => Get(key, @default);
		public int get_int(string key) => Get(key, default(int));
		#endregion

		#region Float
		public void set_float(string key, float value) => Set(key, value);
		public float get_float(string key, float @default) => Get(key, @default);
		public float get_float(string key) => Get(key, default(float));
		#endregion
	}
}
