namespace Cutulu.Core
{
    using System.IO;

    /// <summary>
    /// Use static method Register() to see more.
    /// </summary>
    public static class CutuluByteFormatters
    {
        class hyteFormatter : BinaryEncoder<Int4>
        {
            public override void Encode(BinaryWriter writer, ref object value) => writer.Write(((Int4)value).Byte);
            public override object Decode(BinaryReader reader) => new Int4() { Byte = reader.ReadByte() };
        }

        class qyteFormatter : BinaryEncoder<Int2>
        {
            public override void Encode(BinaryWriter writer, ref object value) => writer.Write(((Int2)value).Byte);
            public override object Decode(BinaryReader reader) => new Int2() { Byte = reader.ReadByte() };
        }

        class middleFormatter : BinaryEncoder<Int24>
        {
            public override void Encode(BinaryWriter writer, ref object value) => writer.Write(((Int24)value).Bytes);
            public override object Decode(BinaryReader reader) => new Int24() { Bytes = reader.ReadBytes(3) };
        }

        class umiddleFormatter : BinaryEncoder<UInt24>
        {
            public override void Encode(BinaryWriter writer, ref object value) => writer.Write(((UInt24)value).Bytes);
            public override object Decode(BinaryReader reader) => new UInt24() { Bytes = reader.ReadBytes(3) };
        }
    }
}