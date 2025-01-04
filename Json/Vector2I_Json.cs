namespace Cutulu.Json
{
    using System.Text.Json.Serialization;
    using System;

    using options = System.Text.Json.JsonSerializerOptions;
    using writer = System.Text.Json.Utf8JsonWriter;
    using reader = System.Text.Json.Utf8JsonReader;
    using i2 = Godot.Vector2I;

    using Core;

    public class Vector2I_Json : JsonConverter<i2>
    {
        public override i2 Read(ref reader reader, Type typeToConvert, options options)
        {
            i2 result = i2.Zero;

            string[] split = reader.GetString().Split('\'', StringSplitOptions.TrimEntries);
            if (split.Length > 0 && int.TryParse(split[0], out int x)) result.X = x;
            if (split.Length > 1 && int.TryParse(split[1], out int y)) result.Y = y;

            return result;
        }

        public override void Write(writer writer, i2 value, options options)
        {
            writer.WriteStringValue($"{value.X}'{value.Y}");
        }
    }
}
