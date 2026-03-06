#if GODOT4_0_OR_GREATER
namespace Cutulu.Core;

using System.IO;
using System;
using Godot;

/// <summary>
/// Use static method Register() to see more.
/// </summary>
public static class GodotEncoders
{
    #region Vector3         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class Vector3IEncoder() : BinaryEncoder(typeof(Vector3I))
    {
        public override void Encode(BinaryWriter writer, Type type, object value)
        {
            Vector3I _ = (Vector3I)value;
            for (int i = 0; i < 3; i++)
            {
                writer.Write(_[i]);
            }
        }

        public override object Decode(BinaryReader reader, Type type)
        {
            return new Vector3I(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }
    }

    class Vector3Formatter() : BinaryEncoder(typeof(Vector3))
    {
        public override void Encode(BinaryWriter writer, Type type, object value)
        {
            var _ = (Vector3)value;
            for (int i = 0; i < 3; i++)
            {
                writer.Write(_[i]);
            }
        }

        public override object Decode(BinaryReader reader, Type type)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
    #endregion

    #region Vector2         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class Vector2IFormatter() : BinaryEncoder(typeof(Vector2I))
    {
        public override void Encode(BinaryWriter writer, Type type, object value)
        {
            Vector2I _ = (Vector2I)value;
            for (int i = 0; i < 2; i++)
            {
                writer.Write(_[i]);
            }
        }

        public override object Decode(BinaryReader reader, Type type)
        {
            return new Vector2I(reader.ReadInt32(), reader.ReadInt32());
        }
    }

    class Vector2Formatter() : BinaryEncoder(typeof(Vector2))
    {
        public override void Encode(BinaryWriter writer, Type type, object value)
        {
            Vector2 _ = (Vector2)value;
            for (int i = 0; i < 2; i++)
            {
                writer.Write(_[i]);
            }
        }

        public override object Decode(BinaryReader reader, Type type)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }
    #endregion

    class ColorFormatter() : BinaryEncoder(typeof(Color))
    {
        public override void Encode(BinaryWriter writer, Type type, object value)
        {
            Color _ = (Color)value;
            for (int i = 0; i < 4; i++)
            {
                writer.Write(_[i]);
            }
        }

        public override object Decode(BinaryReader reader, Type type)
        {
            return new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
#endif