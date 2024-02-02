using System.IO;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Use static method Register() to see more.
    /// </summary>
    public static class GodotByteFormatters
    {
        /// <summary>
        /// Registers formatters for basic structs like Vectors.
        /// </summary>
        public static void Register()
        {
            new Vector3IFormatter().Register<Vector3I>();
            new Vector3Formatter().Register<Vector3>();

            new Vector2IFormatter().Register<Vector2I>();
            new Vector2Formatter().Register<Vector2>();
        }

        #region Vector3         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        class Vector3IFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer)
            {
                Vector3I _ = (Vector3I)value;
                for (int i = 0; i < 3; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Read(BinaryReader reader)
            {
                return new Vector3I(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            }
        }

        class Vector3Formatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer)
            {
                Vector3 _ = (Vector3)value;
                for (int i = 0; i < 3; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Read(BinaryReader reader)
            {
                return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }
        #endregion

        #region Vector2         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        class Vector2IFormatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer)
            {
                Vector2I _ = (Vector2I)value;
                for (int i = 0; i < 2; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Read(BinaryReader reader)
            {
                return new Vector2I(reader.ReadInt32(), reader.ReadInt32());
            }
        }

        class Vector2Formatter : ByteFormatter
        {
            public override void Write(object value, BinaryWriter writer)
            {
                Vector2 _ = (Vector2)value;
                for (int i = 0; i < 2; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Read(BinaryReader reader)
            {
                return new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
        }
        #endregion
    }
}