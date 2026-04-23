namespace Cutulu.Core;

using System.IO;
using System;

public struct StandaloneBuffer
{
    [Encodable] public Type Type;
    [Encodable] public object Value;

    public StandaloneBuffer(object value)
    {
        if (value == null) Type = null;
        else Type = value.GetType();
        Value = value;
    }

    public bool HasValue => Type != null && Value.NotNull();

    public bool TryGetValue<T>(out T value)
    {
        if (HasValue && Value is T v)
        {
            value = v;
            return true;
        }

        value = default;
        return false;
    }

    class Encoder : BinaryEncoder
    {
        public Encoder() : base(typeof(StandaloneBuffer)) { }

        public override void Encode(BinaryWriter writer, Type type, object value)
        {
            if (value is not StandaloneBuffer buffer || buffer.Type == null || buffer.Value == null)
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
            writer.Write(buffer.Type.FullName);
            writer.Encode(buffer.Value);
        }

        public override object Decode(BinaryReader reader, Type type)
        {
            if (reader.ReadBoolean() == false) return new StandaloneBuffer();

            Type _type = Type.GetType(reader.ReadString());
            return new StandaloneBuffer(reader.Decode(_type));
        }
    }
}