namespace Cutulu.Numerics
{
    using System;

    /// <summary>
    /// Represents a number. Dynamic in it's byte size.
    /// </summary>
    public partial struct Number
    {
        public byte[] Buffer { get; set; }

        public readonly NumberType GetNumberType() =>
            Buffer.IsEmpty() ? NumberType.Invalid :
            Buffer.Length == 8 ? NumberType.Long :
            Buffer.Length == 4 ? NumberType.Int :
            Buffer.Length == 2 ? NumberType.Short :
            NumberType.SByte;

        public Number() { Buffer = null; }
        public Number(object value)
        {
            while (true)
            {
                switch (value)
                {
                    case sbyte v:
                        Buffer = v.Encode();
                        return;

                    case byte v:
                        Buffer = v.Encode();
                        return;

                    case short v:
                        if (IsSByte(v))
                        {
                            value = (sbyte)v;
                            break;
                        }

                        // Is short
                        else
                        {
                            Buffer = v.Encode();
                            return;
                        }

                    case int v:
                        if (IsShort(v))
                        {
                            value = (short)v;
                            break;
                        }

                        // Is int
                        else
                        {
                            Buffer = v.Encode();
                            return;
                        }

                    case long v:
                        if (IsInt(v))
                        {
                            value = (int)v;
                            break;
                        }

                        // Is long
                        else
                        {
                            Buffer = v.Encode();
                            return;
                        }

                    default:
                        Buffer = Array.Empty<byte>();
                        return;
                }
            }

            static bool IsSByte(short value) => value <= sbyte.MaxValue && value >= sbyte.MinValue;
            static bool IsShort(int value) => value <= short.MaxValue && value >= short.MinValue;
            static bool IsInt(long value) => value <= int.MaxValue && value >= int.MinValue;
        }

        public readonly int GetInt()
        {
            return GetNumberType() switch
            {
                NumberType.SByte => Buffer.Decode<sbyte>(),
                NumberType.Short => Buffer.Decode<short>(),
                NumberType.Int => Buffer.Decode<int>(),
                NumberType.Long => (int)Buffer.Decode<long>(),
                _ => default,
            };
        }

        class Encoder : BinaryEncoder<Number>
        {
            public override void Encode(System.IO.BinaryWriter writer, ref object value)
            {
                var number = (Number)value;

                writer.Write((byte)number.Buffer.Length);
                writer.Write(number.Buffer);
            }

            public override object Decode(System.IO.BinaryReader reader)
            {
                return new Number() { Buffer = reader.ReadBytes(reader.ReadByte()), };
            }
        }
    }

    public enum NumberType : byte
    {
        Invalid,

        SByte,
        Short,
        Int,
        Long,
    }
}