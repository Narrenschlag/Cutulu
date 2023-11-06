using System.Text.Json.Serialization;
using System;

using options = System.Text.Json.JsonSerializerOptions;
using writer = System.Text.Json.Utf8JsonWriter;
using reader = System.Text.Json.Utf8JsonReader;

namespace Cutulu.JsonConverter
{
	public class CPair_Json<A, B> : JsonConverter<CPair<A, B>>
	{
		public override CPair<A, B> Read(ref reader reader, Type typeToConvert, options options)
		{
			string[] split = reader.GetString().Split('|', StringSplitOptions.TrimEntries);
			return split.Length >= 2 ? new CPair<A, B>(split[0].jsonCurrentFormat<A>(), split[1].jsonCurrentFormat<B>()) : throw new Exception("Cannot deserialize!");
		}

		public override void Write(writer writer, CPair<A, B> value, options options)
		{
			writer.WriteStringValue($"{value.a.jsonCurrentFormat()}|{value.b.jsonCurrentFormat()}");
		}
	}
}
