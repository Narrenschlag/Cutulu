namespace Cutulu
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

        public readonly sbyte GetSByte()
        {
            return GetNumberType() switch
            {
                NumberType.SByte => Buffer.Decode<sbyte>(),
                NumberType.Short => (sbyte)Buffer.Decode<short>(),
                NumberType.Int => (sbyte)Buffer.Decode<int>(),
                NumberType.Long => (sbyte)Buffer.Decode<long>(),
                _ => default,
            };
        }

        public readonly short GetShort()
        {
            return GetNumberType() switch
            {
                NumberType.SByte => Buffer.Decode<sbyte>(),
                NumberType.Short => Buffer.Decode<short>(),
                NumberType.Int => (short)Buffer.Decode<int>(),
                NumberType.Long => (short)Buffer.Decode<long>(),
                _ => default,
            };
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

        public readonly long GetLong()
        {
            return GetNumberType() switch
            {
                NumberType.SByte => Buffer.Decode<sbyte>(),
                NumberType.Short => Buffer.Decode<short>(),
                NumberType.Int => Buffer.Decode<int>(),
                NumberType.Long => Buffer.Decode<long>(),
                _ => default,
            };
        }

        public static byte GetNumberId(params Number[] numbers)
        {
            if (numbers.Length > 4) throw new($"Can only identify up to 4 numbers at a time.");

            var bitBuilder = new BitBuilder(default(byte));

            for (var i = 0; i < numbers.Length; i++)
            {
                switch (numbers[i].GetNumberType())
                {
                    case NumberType.SByte:
                        bitBuilder.AddBinary("00");
                        break;

                    case NumberType.Short:
                        bitBuilder.AddBinary("01");
                        break;

                    case NumberType.Int:
                        bitBuilder.AddBinary("10");
                        break;

                    case NumberType.Long:
                        bitBuilder.AddBinary("11");
                        break;
                }
            }

            bitBuilder.Fill(8, false);

            return bitBuilder.ByteBuffer[0];
        }

        public static NumberType[] ReadNumberId(byte numberId)
        {
            var bitBuilder = new BitBuilder(numberId);
            var result = new NumberType[4];

            for (var i = 0; i < 4; i++)
            {
                result[i] = (NumberType)bitBuilder.GetByte(i * 2, 2);
                result[i]++;
            }

            return result;
        }

        public static implicit operator Number(sbyte value) => new(value);
        public static implicit operator Number(short value) => new(value);
        public static implicit operator Number(int value) => new(value);
        public static implicit operator Number(long value) => new(value);

        public static implicit operator sbyte(Number value) => value.GetSByte();
        public static implicit operator short(Number value) => value.GetShort();
        public static implicit operator int(Number value) => value.GetInt();
        public static implicit operator long(Number value) => value.GetLong();

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