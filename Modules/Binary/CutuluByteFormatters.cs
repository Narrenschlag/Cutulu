using System.IO;

namespace Cutulu
{
    /// <summary>
    /// Use static method Register() to see more.
    /// </summary>
    public static class CutuluByteFormatters
    {
        /// <summary>
        /// Registers formatters for basic structs like Vectors.
        /// </summary>
        public static void Register()
        {
            new hyteFormatter().Register<Int4>();
            new qyteFormatter().Register<Int2>();

            new middleFormatter().Register<Int24>();
            new umiddleFormatter().Register<UInt24>();
        }

        class hyteFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer) => writer.Write(((Int4)value).Byte);
            public override object Read(BinaryReader reader) => new Int4() { Byte = reader.ReadByte() };
        }

        class qyteFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer) => writer.Write(((Int2)value).Byte);
            public override object Read(BinaryReader reader) => new Int2() { Byte = reader.ReadByte() };
        }

        class middleFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer) => writer.Write(((Int24)value).Bytes);
            public override object Read(BinaryReader reader) => new Int24() { Bytes = reader.ReadBytes(3) };
        }

        class umiddleFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer) => writer.Write(((UInt24)value).Bytes);
            public override object Read(BinaryReader reader) => new UInt24() { Bytes = reader.ReadBytes(3) };
        }
    }
}