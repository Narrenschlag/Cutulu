namespace Cutulu
{
    using System;

    /// <summary>
    /// Represents a number. Dynamic in it's byte size.
    /// </summary>
    public partial struct UNumber
    {
        public byte[] Buffer { get; set; }

        public readonly TypeEnum GetTypeEnum() =>
            Buffer.IsEmpty() ? TypeEnum.Invalid :
            Buffer.Length == 8 ? TypeEnum.ULong :
            Buffer.Length == 4 ? TypeEnum.UInt :
            Buffer.Length == 2 ? TypeEnum.UShort :
            TypeEnum.Byte;

        public UNumber() { Buffer = null; }
        public UNumber(object value)
        {
            while (true)
            {
                switch (value)
                {
                    case byte v:
                        Buffer = v.Encode();
                        return;

                    case ushort v:
                        if (IsByte(ref v))
                        {
                            value = (byte)v;
                            break;
                        }

                        // Is ushort
                        else
                        {
                            Buffer = v.Encode();
                            return;
                        }

                    case uint v:
                        if (IsUShort(ref v))
                        {
                            value = (short)v;
                            break;
                        }

                        // Is uint
                        else
                        {
                            Buffer = v.Encode();
                            return;
                        }

                    case ulong v:
                        if (IsUInt(ref v))
                        {
                            value = (int)v;
                            break;
                        }

                        // Is ulong
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

            static bool IsByte(ref ushort value) => value <= byte.MaxValue && value >= byte.MinValue;
            static bool IsUShort(ref uint value) => value <= ushort.MaxValue && value >= ushort.MinValue;
            static bool IsUInt(ref ulong value) => value <= uint.MaxValue && value >= uint.MinValue;
        }

        public readonly byte GetByte()
        {
            return GetTypeEnum() switch
            {
                TypeEnum.Byte => Buffer.Decode<byte>(),
                TypeEnum.UShort => (byte)Buffer.Decode<ushort>(),
                TypeEnum.UInt => (byte)Buffer.Decode<uint>(),
                TypeEnum.ULong => (byte)Buffer.Decode<ulong>(),
                _ => default,
            };
        }

        public readonly ushort GetUShort()
        {
            return GetTypeEnum() switch
            {
                TypeEnum.Byte => Buffer.Decode<byte>(),
                TypeEnum.UShort => Buffer.Decode<ushort>(),
                TypeEnum.UInt => (ushort)Buffer.Decode<uint>(),
                TypeEnum.ULong => (ushort)Buffer.Decode<ulong>(),
                _ => default,
            };
        }

        public readonly uint GetUInt()
        {
            return GetTypeEnum() switch
            {
                TypeEnum.Byte => Buffer.Decode<byte>(),
                TypeEnum.UShort => Buffer.Decode<ushort>(),
                TypeEnum.UInt => Buffer.Decode<uint>(),
                TypeEnum.ULong => (uint)Buffer.Decode<ulong>(),
                _ => default,
            };
        }

        public readonly ulong GetULong()
        {
            return GetTypeEnum() switch
            {
                TypeEnum.Byte => Buffer.Decode<byte>(),
                TypeEnum.UShort => Buffer.Decode<ushort>(),
                TypeEnum.UInt => Buffer.Decode<uint>(),
                TypeEnum.ULong => Buffer.Decode<ulong>(),
                _ => default,
            };
        }

        public static byte GetNumberId(params UNumber[] numbers)
        {
            if (numbers.Length > 4) throw new($"Can only identify up to 4 numbers at a time.");

            var bitBuilder = new BitBuilder(default(byte));

            for (var i = 0; i < numbers.Length; i++)
            {
                switch (numbers[i].GetTypeEnum())
                {
                    case TypeEnum.Byte:
                        bitBuilder.AddBinary("00");
                        break;

                    case TypeEnum.UShort:
                        bitBuilder.AddBinary("01");
                        break;

                    case TypeEnum.UInt:
                        bitBuilder.AddBinary("10");
                        break;

                    case TypeEnum.ULong:
                        bitBuilder.AddBinary("11");
                        break;
                }
            }

            bitBuilder.Fill(8, false);

            return bitBuilder.ByteBuffer[0];
        }

        public static TypeEnum[] ReadNumberId(byte numberId)
        {
            var bitBuilder = new BitBuilder(numberId);
            var result = new TypeEnum[4];

            for (var i = 0; i < 4; i++)
            {
                result[i] = (TypeEnum)bitBuilder.GetByte(i * 2, 2);
                result[i]++;
            }

            return result;
        }

        public static implicit operator UNumber(byte value) => new(value);
        public static implicit operator UNumber(ushort value) => new(value);
        public static implicit operator UNumber(uint value) => new(value);
        public static implicit operator UNumber(ulong value) => new(value);

        public static implicit operator byte(UNumber value) => value.GetByte();
        public static implicit operator ushort(UNumber value) => value.GetUShort();
        public static implicit operator uint(UNumber value) => value.GetUInt();
        public static implicit operator ulong(UNumber value) => value.GetULong();

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

        public enum TypeEnum : byte
        {
            Invalid,

            Byte,
            UShort,
            UInt,
            ULong,
        }
    }
}