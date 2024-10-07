namespace Cutulu
{
    /// <summary>
    /// Represents a vector. Dynamic in it's byte size.
    /// </summary>
    public partial struct Number2I
    {
        public readonly int Y => Vector.Numbers.Length > 1 ? Vector.Numbers[1].GetUInt() : 0;
        public readonly int X => Vector.Numbers[0].GetUInt();

        private NumberXI Vector;

        public Number2I() { Vector = new NumberXI(0, 0); }

        public Number2I(Godot.Vector2I vector) : this(vector.X, vector.Y) { }
        public Number2I(int x, int y)
        {
            Vector = new NumberXI(x, y);
        }

        public readonly Godot.Vector2I Godot => Vector.GetVector2I();

        public static implicit operator Godot.Vector2I(Number2I value) => value.Godot;
        public static implicit operator Number2I(Godot.Vector2I value) => new(value);

        class VectorI2Encoder : BinaryEncoder<Number2I>
        {
            public override void Encode(System.IO.BinaryWriter writer, ref object value)
            {
                var numbers = ((Number2I)value).Vector.Numbers;
                writer.Write(Number.GetNumberId(numbers));

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
                var vector = new Number2I() { Vector = new() { Numbers = new Number[2] } };
                var byteCount = Number.ReadNumberId(reader.ReadByte());

                for (byte i = 0; i < 2; i++)
                {
                    vector.Vector.Numbers[i] = new() { Buffer = reader.ReadBytes((int)byteCount[i]), };
                }

                return vector;
            }
        }
    }
}