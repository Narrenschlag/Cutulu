#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using System.IO;
    using Godot;

    public struct Direction
    {
        public byte[] Values { get; set; }

        public Vector3 Vector;

        public Direction(CharacterBody3D body) : this(body.Velocity) { }
        public Direction(Node3D node) : this(node.Forward()) { }

        public Direction(Vector3 vector)
        {
            Vector = vector.Round(0.01f).Normalized();

            Values = new[] { Vector[0].FloatToByte(), Vector[1].FloatToByte(), Vector[2].FloatToByte() };
        }

        public Direction(byte[] bytes)
        {
            Values = bytes;

            Vector = new Vector3(bytes[0].ByteToFloat(), bytes[1].ByteToFloat(), bytes[2].ByteToFloat()).Round(0.01f).Normalized();
        }

        class directionModifier : BinaryEncoder<Direction>
        {
            public override void Encode(BinaryWriter writer, ref object value) => writer.Write(((Direction)value).Values);
            public override object Decode(BinaryReader reader) => new Direction(reader.ReadBytes(3));
        }
    }
}
#endif