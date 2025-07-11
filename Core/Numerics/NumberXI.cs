#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using Godot;

    /// <summary>
    /// Represents a vector. Dynamic in it's byte size.
    /// </summary>
    public partial struct NumberXI
    {
        public Number[] Numbers { get; set; }

        public NumberXI() { Numbers = new Number[2] { default, default, }; }
        public NumberXI(int x, int y, params int[] z)
        {
            if (z.IsEmpty())
            {
                Numbers = new Number[] { new(x), new(y) };
            }

            else
            {
                Numbers = new Number[2 + z.Length];
                Numbers[0] = new(x);
                Numbers[1] = new(y);

                for (var i = 0; i < z.Length; i++)
                {
                    Numbers[i + 2] = new(z[i]);
                }
            }
        }

        public readonly Vector2I GetVector2I()
        {
            return new(Numbers[0].GetInt(), Numbers.Length > 1 ? Numbers[1].GetInt() : 0);
        }

        public readonly Vector3I GetVector3I()
        {
            return new Vector3I(Numbers[0].GetInt(), Numbers.Length > 1 ? Numbers[1].GetInt() : 0, Numbers.Length > 2 ? Numbers[2].GetInt() : 0);
        }

        public readonly Vector4I GetVector4I()
        {
            return new Vector4I(Numbers[0].GetInt(), Numbers.Length > 1 ? Numbers[1].GetInt() : 0, Numbers.Length > 2 ? Numbers[2].GetInt() : 0, Numbers.Length > 3 ? Numbers[3].GetInt() : 0);
        }

        public readonly int[] GetVectorI(int count)
        {
            var vector = new int[count];

            for (int i = 0; i < count; i++)
            {
                vector[i] = Numbers[i].GetInt();
            }

            return vector;
        }

        class Encoder : BinaryEncoder<NumberXI>
        {
            public override void Encode(System.IO.BinaryWriter writer, ref object value)
            {
                var numbers = ((NumberXI)value).Numbers;

                writer.Write((byte)numbers[0].Buffer.Length);
                writer.Write((byte)numbers.Length);

                for (var i = 0; i < numbers.Length; i++)
                {
                    writer.Write(numbers[i].Buffer);
                }
            }

            public override object Decode(System.IO.BinaryReader reader)
            {
                var bytes = reader.ReadByte();
                var count = reader.ReadByte();

                var vector = new NumberXI() { Numbers = new Number[count] };
                for (var i = 0; i < count; i++)
                {
                    vector.Numbers[i] = new()
                    {
                        Buffer = reader.ReadBytes(bytes),
                    };
                }

                return vector;
            }
        }
    }
}
#endif