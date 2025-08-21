namespace Cutulu.Core
{
    using System;

    /// <summary>
    /// Represents a number. Dynamic in it's byte size.
    /// </summary>
    public readonly struct UNumber
    {
        public readonly ulong Value;

        public readonly TypeEnum GetTypeEnum() =>
            Value > uint.MaxValue ? TypeEnum.ULong :
            Value > ushort.MaxValue ? TypeEnum.UInt :
            Value > byte.MaxValue ? TypeEnum.UShort :
            TypeEnum.Byte;

        public static byte GetLength(TypeEnum _type)
        {
            return _type switch
            {
                TypeEnum.UShort => 2,
                TypeEnum.UInt => 4,
                TypeEnum.ULong => 8,
                _ => 1,
            };
        }

        public override readonly string ToString() => Value.ToString();

        public UNumber(ulong value) => Value = value;
        public UNumber() => Value = default;

        private byte[] GetBuffer()
        {
            return GetTypeEnum() switch
            {
                TypeEnum.Byte => [(byte)Value],
                TypeEnum.UShort => BitConverter.GetBytes((ushort)Value),
                TypeEnum.UInt => BitConverter.GetBytes((uint)Value),
                TypeEnum.ULong => BitConverter.GetBytes(Value),
                _ => [],
            };
        }

        public static implicit operator UNumber(byte value) => new(value);
        public static implicit operator UNumber(short value) => new(value < 0 ? default : (ushort)value);
        public static implicit operator UNumber(ushort value) => new(value);
        public static implicit operator UNumber(int value) => new(value < 0 ? default : (uint)value);
        public static implicit operator UNumber(uint value) => new(value);
        public static implicit operator UNumber(long value) => new(value < 0 ? default : (ulong)value);
        public static implicit operator UNumber(ulong value) => new(value);

        public static implicit operator byte(UNumber value) => (byte)value.Value;
        public static implicit operator ushort(UNumber value) => (ushort)value.Value;
        public static implicit operator short(UNumber value) => (short)value.Value;
        public static implicit operator uint(UNumber value) => (uint)value.Value;
        public static implicit operator int(UNumber value) => (int)value.Value;
        public static implicit operator ulong(UNumber value) => value.Value;
        public static implicit operator long(UNumber value) => (long)value.Value;

        class Encoder : BinaryEncoder<UNumber>
        {
            private const byte DIV = 252;

            public override void Encode(System.IO.BinaryWriter writer, ref object value)
            {
                var _number = (UNumber)value;

                var _buffer = _number.GetBuffer();

                if (_buffer.Length < 1 || _buffer.Length > 8) writer.Write((byte)0);
                else if (_number < DIV && _buffer.Length == 1) writer.Write((byte)_number);
                else
                {
                    writer.Write((byte)(_number.GetTypeEnum() + DIV - 1));
                    writer.Write(_buffer);
                }
            }

            public override object Decode(System.IO.BinaryReader reader)
            {
                var _byte = reader.ReadByte();

                if (_byte < DIV) return new UNumber(_byte);
                else
                {
                    var _type = (TypeEnum)(_byte - DIV + 1);
                    var _buffer = reader.ReadBytes(GetLength(_type));

                    switch (_type)
                    {
                        case TypeEnum.Byte: return (UNumber)_buffer.Decode<byte>();
                        case TypeEnum.UShort: return (UNumber)_buffer.Decode<ushort>();
                        case TypeEnum.UInt: return (UNumber)_buffer.Decode<uint>();
                        case TypeEnum.ULong: return (UNumber)_buffer.Decode<ulong>();
                    }
                }

                return default;
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