namespace Cutulu.Core
{
    using System;
    using Godot;

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

                    case short v:
                        if (v <= byte.MaxValue) value = (byte)Mathf.Max(v, 0);
                        else value = (ushort)Mathf.Max(v, 0);
                        break;

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

                    case int v:
                        if (v <= byte.MaxValue) value = (byte)Mathf.Max(v, 0);
                        else if (v <= ushort.MaxValue) value = (ushort)Mathf.Max(v, 0);
                        else value = (uint)Mathf.Max(v, 0);
                        break;

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

                    case long v:
                        if (v <= byte.MaxValue) value = (byte)Mathf.Max(v, 0);
                        else if (v <= ushort.MaxValue) value = (ushort)Mathf.Max(v, 0);
                        else if (v <= uint.MaxValue) value = (uint)Mathf.Max(v, 0);
                        else value = (ulong)Mathf.Max(v, 0);
                        break;

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
        public static implicit operator UNumber(short value) => new(value);
        public static implicit operator UNumber(ushort value) => new(value);
        public static implicit operator UNumber(int value) => new(value);
        public static implicit operator UNumber(uint value) => new(value);
        public static implicit operator UNumber(long value) => new(value);
        public static implicit operator UNumber(ulong value) => new(value);

        public static implicit operator byte(UNumber value) => value.GetByte();
        public static implicit operator ushort(UNumber value) => value.GetUShort();
        public static implicit operator short(UNumber value) => (short)value.GetUShort();
        public static implicit operator uint(UNumber value) => value.GetUInt();
        public static implicit operator int(UNumber value) => (int)value.GetUInt();
        public static implicit operator ulong(UNumber value) => value.GetULong();
        public static implicit operator long(UNumber value) => (long)value.GetULong();

        class Encoder : BinaryEncoder<UNumber>
        {
            public override void Encode(System.IO.BinaryWriter writer, ref object value)
            {
                var _number = (UNumber)value;

                if (_number < 128) writer.Write((byte)_number);

                else
                {
                    var _builder = new BitBuilder((byte)_number.Buffer.Length)
                    {
                        [7] = true
                    };

                    writer.Write(_builder.ByteBuffer);
                    writer.Write(_number.Buffer);
                }
            }

            public override object Decode(System.IO.BinaryReader reader)
            {
                var _byte = reader.ReadByte();

                if (_byte < 128) return new UNumber(_byte);

                var _builder = new BitBuilder(_byte)
                {
                    [7] = false
                };

                return new UNumber() { Buffer = reader.ReadBytes(_builder.ByteBuffer[0]), };
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