namespace Cutulu.Numerics
{
    /// <summary>
    /// Represents a vector. Dynamic in it's byte size.
    /// </summary>
    public partial struct Vector2I
    {
        public int Y => Vector.Numbers.Length > 1 ? Vector.Numbers[1].GetInt() : 0;
        public int X => Vector.Numbers[0].GetInt();

        public VectorI Vector;

        public Vector2I() { Vector = new VectorI(0, 0); }

        public Vector2I(int x, int y)
        {
            Vector = new VectorI(x, y);
        }

        public readonly Godot.Vector2I Value => Vector.GetVector2I();
    }

    public class VectorI2Encoder : BinaryEncoder<Vector2I>
    {
        public override void Encode(System.IO.BinaryWriter writer, ref object value)
        {
            var vector = (Vector2I)value;

            writer.Write((byte)vector.Vector.Numbers[0].Buffer.Length);

            for (var i = 0; i < 2; i++)
            {
                writer.Write(vector.Vector.Numbers[i].Buffer);
            }
        }

        public override object Decode(System.IO.BinaryReader reader)
        {
            var bytes = reader.ReadByte();

            var numbers = new Number[2];
            for (var i = 0; i < 2; i++)
            {
                numbers[i] = new() { Buffer = reader.ReadBytes(bytes), };
            }

            return new Vector2I()
            {
                Vector = new VectorI() { Numbers = new[] { numbers[0], numbers[1] } },
            };
        }
    }
}