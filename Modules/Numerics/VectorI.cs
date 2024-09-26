namespace Cutulu.Numerics
{
    using Godot;

    /// <summary>
    /// Represents a vector. Dynamic in it's byte size.
    /// </summary>
    public partial struct VectorI
    {
        public Number[] Numbers { get; set; }

        public VectorI() { Numbers = new Number[2] { default, default, }; }
        public VectorI(int x, int y, params int[] z)
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

        public readonly Godot.Vector2I GetVector2I()
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
    }

    public class VectorIEncoder : BinaryEncoder<VectorI>
    {
        public override void Encode(System.IO.BinaryWriter writer, ref object value)
        {
            var vector = (VectorI)value;

            writer.Write((byte)vector.Numbers[0].Buffer.Length);
            writer.Write((byte)vector.Numbers.Length);

            for (var i = 0; i < vector.Numbers.Length; i++)
            {
                writer.Write(vector.Numbers[i].Buffer);
            }
        }

        public override object Decode(System.IO.BinaryReader reader)
        {
            var bytes = reader.ReadByte();
            var count = reader.ReadByte();

            var numbers = new Number[count];
            for (var i = 0; i < count; i++)
            {
                numbers[i] = new() { Buffer = reader.ReadBytes(bytes), };
            }

            return new VectorI()
            {
                Numbers = numbers,
            };
        }
    }
}