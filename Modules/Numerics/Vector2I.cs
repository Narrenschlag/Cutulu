namespace Cutulu.Numerics
{
    /// <summary>
    /// Represents a vector. Dynamic in it's byte size.
    /// </summary>
    public partial struct Vector2I
    {
        public readonly int Y => Vector.Numbers.Length > 1 ? Vector.Numbers[1].GetInt() : 0;
        public readonly int X => Vector.Numbers[0].GetInt();

        private VectorI Vector;

        public Vector2I() { Vector = new VectorI(0, 0); }

        public Vector2I(Godot.Vector2I vector) : this(vector.X, vector.Y) { }
        public Vector2I(int x, int y)
        {
            Vector = new VectorI(x, y);
        }

        public readonly Godot.Vector2I Godot => Vector.GetVector2I();

        class VectorI2Encoder : BinaryEncoder<Vector2I>
        {
            public override void Encode(System.IO.BinaryWriter writer, ref object value)
            {
                var numbers = ((Vector2I)value).Vector.Numbers;
                writer.Write((byte)numbers[0].Buffer.Length);

                for (byte i = 0; i < 2; i++)
                {
                    if (numbers.Length <= i)
                        writer.Write(new byte[numbers[0].Buffer.Length]);

                    else
                        writer.Write(numbers[i].Buffer);
                }
            }

            public override object Decode(System.IO.BinaryReader reader)
            {
                var vector = new Vector2I() { Vector = new() { Numbers = new Number[2] } };
                var byteCount = reader.ReadByte();

                for (byte i = 0; i < 2; i++)
                {
                    vector.Vector.Numbers[i] = new() { Buffer = reader.ReadBytes(byteCount), };
                }

                return vector;
            }
        }
    }
}