#if GODOT4_0_OR_GREATER
namespace Cutulu.Json
{
    using System.Text.Json.Serialization;
    using System;

    using options = System.Text.Json.JsonSerializerOptions;
    using writer = System.Text.Json.Utf8JsonWriter;
    using reader = System.Text.Json.Utf8JsonReader;
    using System.Collections.Generic;

    using Core;

    public class Dictionary_Json<A, B> : JsonConverter<Dictionary<A, B>>
    {
        public override Dictionary<A, B> Read(ref reader reader, Type typeToConvert, options options)
        {
            return json(reader.GetString());
        }

        public override void Write(writer writer, Dictionary<A, B> value, options options)
        {
            writer.WriteStringValue(json(value));
        }

        private string json(Dictionary<A, B> dic)
        {
            if (dic.IsEmpty()) return null;

            CPair<A, B>[] array = new CPair<A, B>[dic.Count];
            int index = 0;

            foreach (KeyValuePair<A, B> pair in dic)
                array[index++] = new CPair<A, B>(pair.Key, pair.Value);

            return array.json();
        }

        private Dictionary<A, B> json(string json, bool tolerateCorruption = true)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            CPair<A, B>[] array = json.json<CPair<A, B>[]>();
            Dictionary<A, B> dic = new Dictionary<A, B>();

            foreach (CPair<A, B> pair in array)
            {
                try
                {
                    dic.Add(pair.a, pair.b);
                }
                catch
                {
                    if (tolerateCorruption) continue;
                    else throw new Exception("Dictionary could not be read");
                }
            }

            return dic;
        }
    }

    public struct CPair<A, B>
    {
        public A a { get; set; }
        public B b { get; set; }

        public CPair(A a, B b)
        {
            this.a = a;
            this.b = b;
        }
    }
}
#endif