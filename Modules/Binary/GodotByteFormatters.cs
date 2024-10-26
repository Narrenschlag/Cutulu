using System.IO;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Use static method Register() to see more.
    /// </summary>
    public static class GodotByteFormatters
    {
        #region Vector3         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        class Vector3IEncoder : BinaryEncoder<Vector3I>
        {
            public override void Encode(BinaryWriter writer, ref object value)
            {
                Vector3I _ = (Vector3I)value;
                for (int i = 0; i < 3; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Decode(BinaryReader reader)
            {
                return new Vector3I(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            }
        }

        class Vector3Formatter : BinaryEncoder<Vector3>
        {
            public override void Encode(BinaryWriter writer, ref object value)
            {
                var _ = (Vector3)value;
                for (int i = 0; i < 3; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Decode(BinaryReader reader)
            {
                return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }
        #endregion

        #region Vector2         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        class Vector2IFormatter : BinaryEncoder<Vector2I>
        {
            public override void Encode(BinaryWriter writer, ref object value)
            {
                Vector2I _ = (Vector2I)value;
                for (int i = 0; i < 2; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Decode(BinaryReader reader)
            {
                return new Vector2I(reader.ReadInt32(), reader.ReadInt32());
            }
        }

        class Vector2Formatter : BinaryEncoder<Vector2>
        {
            public override void Encode(BinaryWriter writer, ref object value)
            {
                Vector2 _ = (Vector2)value;
                for (int i = 0; i < 2; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Decode(BinaryReader reader)
            {
                return new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
        }
        #endregion

        class ColorFormatter : BinaryEncoder<Color>
        {
            public override void Encode(BinaryWriter writer, ref object value)
            {
                Color _ = (Color)value;
                for (int i = 0; i < 4; i++)
                {
                    writer.Write(_[i]);
                }
            }

            public override object Decode(BinaryReader reader)
            {
                return new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }
    }
}