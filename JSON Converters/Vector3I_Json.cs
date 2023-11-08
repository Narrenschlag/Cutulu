using System.Text.Json.Serialization;
using System;

using options = System.Text.Json.JsonSerializerOptions;
using writer = System.Text.Json.Utf8JsonWriter;
using reader = System.Text.Json.Utf8JsonReader;
using i3 = Godot.Vector3I;

namespace Cutulu.JsonConverter
{
	public class Vector3I_Json : JsonConverter<i3>
	{
		public override i3 Read(ref reader reader, Type typeToConvert, options options)
		{
			i3 result = i3.Zero;

			string[] split = reader.GetString().Split('\'', StringSplitOptions.TrimEntries);
			if (split.Length > 0 && int.TryParse(split[0], out int x)) result.X = x;
			if (split.Length > 1 && int.TryParse(split[1], out int y)) result.Y = y;
			if (split.Length > 2 && int.TryParse(split[2], out int z)) result.Z = z;

			return result;
		}

		public override void Write(writer writer, i3 value, options options)
		{
			writer.WriteStringValue($"{value.X}'{value.Y}'{value.Z}");
		}
	}
}
