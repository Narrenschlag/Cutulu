namespace Cutulu.Core
{
    using Godot;

    /// <summary>
    /// Represents a number. Dynamic in it's byte size.
    /// </summary>
    public partial struct UNumber
    {
        public byte[] Buffer { get; set; }

        public readonly TypeEnum GetTypeEnum() =>
            Buffer.IsEmpty() || Buffer.Length > 8 ? TypeEnum.Invalid :
            Buffer.Length == 8 ? TypeEnum.ULong :
            Buffer.Length == 4 ? TypeEnum.UInt :
            Buffer.Length == 2 ? TypeEnum.UShort :
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

        public UNumber() { Buffer = null; }
        public UNumber(object _value)
        {
            Buffer = GetBuffer(_value);
        }

        private static byte[] GetBuffer(object _obj)
        {
            // Convert signed to unsigned
            switch (_obj)
            {
                case sbyte v:
                    _obj = (byte)Mathf.Max(v, 0);
                    break;

                case short v:
                    _obj = (ushort)Mathf.Max(v, 0);
                    break;

                case int v:
                    _obj = (uint)Mathf.Max(v, 0);
                    break;

                case long v:
                    _obj = (ulong)Mathf.Max(v, 0);
                    break;
            }

            // Reduce to lowest type
            switch (_obj)
            {
                case byte: break;

                case ushort v:
                    if (v <= byte.MaxValue) _obj = (byte)v;
                    break;

                case uint v:
                    if (v <= byte.MaxValue) _obj = (byte)v;
                    else if (v <= ushort.MaxValue) _obj = (ushort)v;
                    break;

                case ulong v:
                    if (v <= byte.MaxValue) _obj = (byte)v;
                    else if (v <= ushort.MaxValue) _obj = (ushort)v;
                    else if (v <= uint.MaxValue) _obj = (uint)v;
                    break;

                default:
                    _obj = default(byte);
                    break;
            }

            return _obj.Encode();
        }

        private readonly ulong GetValue()
        {
            return GetTypeEnum() switch
            {
                TypeEnum.Byte => Buffer.Decode<byte>(),
                TypeEnum.UShort => Buffer.Decode<ushort>(),
                TypeEnum.UInt => Buffer.Decode<uint>(),
                TypeEnum.ULong => Buffer.Decode<ulong>(),
                _ => default(byte),
            };
        }

        public static implicit operator UNumber(byte value) => new(value);
        public static implicit operator UNumber(short value) => new(value);
        public static implicit operator UNumber(ushort value) => new(value);
        public static implicit operator UNumber(int value) => new(value);
        public static implicit operator UNumber(uint value) => new(value);
        public static implicit operator UNumber(long value) => new(value);
        public static implicit operator UNumber(ulong value) => new(value);

        public static implicit operator byte(UNumber value) => (byte)value.GetValue();
        public static implicit operator ushort(UNumber value) => (ushort)value.GetValue();
        public static implicit operator short(UNumber value) => (short)value.GetValue();
        public static implicit operator uint(UNumber value) => (uint)value.GetValue();
        public static implicit operator int(UNumber value) => (int)value.GetValue();
        public static implicit operator ulong(UNumber value) => value.GetValue();
        public static implicit operator long(UNumber value) => (long)value.GetValue();

        public new readonly string ToString() => ((ulong)this).ToString();

        class Encoder : BinaryEncoder<UNumber>
        {
            private const byte DIV = 252;

            public override void Encode(System.IO.BinaryWriter writer, ref object value)
            {
                var _number = (UNumber)value;

                if (_number.Buffer.Length < 1 || _number.Buffer.Length > 8) writer.Write((byte)0);
                else if (_number < DIV && _number.Buffer.Length == 1) writer.Write((byte)_number);
                else
                {
                    writer.Write((byte)(_number.GetTypeEnum() + DIV - 1));
                    writer.Write(_number.Buffer);
                }
            }

            public override object Decode(System.IO.BinaryReader reader)
            {
                var _byte = reader.ReadByte();

                return _byte < DIV ? new UNumber(_byte) :
                new() { Buffer = reader.ReadBytes(GetLength((TypeEnum)(_byte - DIV + 1))), };
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