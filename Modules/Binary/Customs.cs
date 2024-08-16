using System.IO;
using Godot;

namespace Cutulu
{
    public struct ColorRGB
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public ColorRGB(Color color)
        {
            R = (byte)color.R8;
            G = (byte)color.G8;
            B = (byte)color.B8;
        }

        public ColorRGB(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public readonly Color RGB32(float alpha = 1) => new(R / 255f, G / 255f, B / 255f, alpha);

        public class Formatter : BinaryEncoder<ColorRGB>
        {
            public override void Encode(BinaryWriter writer, ref object value)
            {
                var obj = (ColorRGB)value;

                writer.Write(obj.R);
                writer.Write(obj.G);
                writer.Write(obj.B);
            }

            public override object Decode(BinaryReader reader) => new ColorRGB(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }
    }

    public struct Vector2S
    {
        public short X { get; set; }
        public short Y { get; set; }

        public Vector2S(short x, short y)
        {
            X = x;
            Y = y;
        }

        public Vector3 ToVector3(float y = 0) => new(X, y, Y);
        public Vector2 ToVector2() => new(X, Y);

        public class Formatter : BinaryEncoder<Vector2S>
        {
            public override void Encode(BinaryWriter writer, ref object value)
            {
                var obj = (Vector2S)value;

                writer.Write(obj.X);
                writer.Write(obj.Y);
            }

            public override object Decode(BinaryReader reader) => new Vector2S(reader.ReadInt16(), reader.ReadInt16());
        }
    }

    public struct Vector38
    {
        public byte[] Values;

        public Vector38(byte x, byte y, byte z) => Values = new byte[3] { x, y, z };

        public class Formatter : BinaryEncoder<Vector38>
        {
            public override void Encode(BinaryWriter writer, ref object value) => writer.Write(((Vector38)value).Values);

            public override object Decode(BinaryReader reader) => new Vector38(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }
    }
}