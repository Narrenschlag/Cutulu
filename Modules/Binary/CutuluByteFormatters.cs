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
            new hyteFormatter().Register<hyte>();
            new qyteFormatter().Register<qyte>();

            new middleFormatter().Register<middle>();
            new umiddleFormatter().Register<umiddle>();
        }

        class hyteFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer) => writer.Write(((hyte)value).Byte);
            public override object Read(BinaryReader reader) => new hyte() { Byte = reader.ReadByte() };
        }

        class qyteFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer) => writer.Write(((qyte)value).Byte);
            public override object Read(BinaryReader reader) => new qyte() { Byte = reader.ReadByte() };
        }

        class middleFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer) => writer.Write(((middle)value).Bytes);
            public override object Read(BinaryReader reader) => new middle() { Bytes = reader.ReadBytes(3) };
        }

        class umiddleFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer) => writer.Write(((umiddle)value).Bytes);
            public override object Read(BinaryReader reader) => new umiddle() { Bytes = reader.ReadBytes(3) };
        }
    }
}