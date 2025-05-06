namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using System.IO;
    using System;

    /// <summary>
    /// Static class for encoding and decoding binary data
    /// </summary>
    public static class Encoder
    {
        #region Encoders [1/4]

        private static readonly Dictionary<Type, IBinaryEncoder> Encoders = [];
        public static int EncoderCount => Encoders.Count;

        public static string LastPropertyName { get; private set; }
        public static Type LastPropertyType { get; private set; }

        static Encoder()
        {
            RegisterAllEncoders();
        }

        private static void RegisterAllEncoders()
        {
            // Get the assembly where BinaryEncoder<T> implementations are located
            var assembly = Assembly.GetExecutingAssembly();

            // Find all encoder types
            var encoderTypes = EncoderFinder.FindEncoders(assembly);

            // Instantiate each encoder and add it to the dictionary
            foreach (var encoderType in encoderTypes)
            {
                // Get the generic type argument from the encoder
                var encoderGenericType = encoderType.BaseType.GetGenericArguments().FirstOrDefault();

                if (encoderGenericType != null && Activator.CreateInstance(encoderType) is IBinaryEncoder encoderInstance)
                {
                    // Skip inactive encoders
                    if (encoderInstance.Active() == false) continue;

                    // Assign to the Encoders dictionary
                    if (Encoders.TryGetValue(encoderGenericType, out var encoder) == false || encoder.Priority() <= encoderInstance.Priority())
                        Encoders[encoderGenericType] = encoderInstance;
                }
            }
        }

        #endregion

        #region Input [2/4]

        /// <summary>
        /// Encodes an object into a byte array
        /// </summary>
        public static byte[] Encode<T>(this T obj)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);

            // Write empty array
            if (obj == null && typeof(T).IsArray) writer.Write(default(ushort));

            // Write object
            else Encode(writer, obj, obj is byte[]);

            // Return result
            return memory.ToArray() ?? [];
        }

        /// <summary>
        /// Encodes an object into a byte array and writes it to the BinaryWriter
        /// <code>
        /// raw = true: Writes byte[] as is without trying to encode it
        /// </code>
        /// </summary>
        public static bool Encode(BinaryWriter writer, object obj, bool raw = false)
        {
            if (obj == null) return false;

            switch (obj)
            {
                case byte[] v:
                    if (raw == false) writer.Write(new UNumber(v.Length).Encode());
                    writer.Write(v); break;

                case string v: writer.Write(v); break;
                case bool v: writer.Write(v); break;
                case char v: writer.Write(v); break;

                case long v: writer.Write(v); break;
                case ulong v: writer.Write(v); break;

                case int v: writer.Write(v); break;
                case uint v: writer.Write(v); break;

                case short v: writer.Write(v); break;
                case ushort v: writer.Write(v); break;

                case byte v: writer.Write(v); break;
                case sbyte v: writer.Write(v); break;

                case double v: writer.Write(v); break;
                case float v: writer.Write(v); break;

                default:
                    // Encode using custom encoder
                    if (Encoders.TryGetValue(obj.GetType(), out var encoder))
                    {
                        encoder.Encode(writer, ref obj);
                    }

                    // Encode types without serializer
                    else if (EncodeUnknown(writer, ref obj) == false)
                    {
                        return false;
                    }

                    break;
            }

            return true;
        }

        private static bool EncodeUnknown(BinaryWriter _writer, ref object _obj)
        {
            if (_obj == null) return false;
            var _type = _obj.GetType();

            // Encode enum
            if (_type.IsEnum)
            {
                _writer.Write(Encode(Convert.ChangeType(_obj, _type.GetEnumUnderlyingType())));
            }

            // Arrays
            else if (_type.IsArray && _obj is Array array)
            {
                _writer.Write(new UNumber(array.Length).Encode());
                _type = _type.GetElementType();

                for (ushort i = 0; i < array.Length; i++)
                {
                    var _value = array.GetValue(i);

                    // Write null array as empty array
                    if (_value == null && _type.IsArray) _writer.Write(default(ushort));

                    // Write array value
                    else _writer.Write(Encode(_value));
                }
            }

            // Classes and structs
            else
            {
                var _manager = PropertyManager.Open(_type);

                for (ushort i = 0; i < _manager.Properties.Length; i++)
                {
                    // Skip properties that have [DontEncode] attribute
                    if (((DontEncode[])_manager.GetInfo(i).GetCustomAttributes(typeof(DontEncode))).Length > 0)
                        continue;

                    var _value = _manager.GetValue(_obj, i);
                    _type = _manager.GetType(i);

                    // Write value
                    if (_value == null) _writer.Write(
                            _type.IsArray ? new byte[2] : // Write null array as empty array
                            new byte[1]); // Write null string as empty byte
                    else Encode(_writer, _value); // Encode value as usual
                }
            }

            return true;
        }

        #endregion

        #region Output [3/4]

        /// <summary>
        /// Decodes a byte array into an object
        /// </summary>
        public static T Decode<T>(this byte[] _buffer)
        {
            var _obj = Decode(_buffer, typeof(T));
            return _obj is T _t ? _t : default;
        }

        /// <summary>
        /// Decodes a byte array into an object
        /// </summary>
        public static object Decode(this byte[] _buffer, Type _type)
        {
            using var memory = new MemoryStream(_buffer);
            using var reader = new BinaryReader(memory);

            return Decode(reader, _type, true);
        }

        /// <summary>
        /// Decodes a byte array from the BinaryReader into an object
        /// </summary>
        public static T Decode<T>(this BinaryReader reader)
        {
            return (T)Decode(reader, typeof(T), true);
        }

        private static object Decode(this BinaryReader reader, Type type, bool _raw = false)
        {
            LastPropertyType = type;

            return type switch
            {
                var t when t == typeof(byte[]) => _raw ? reader.ReadRemainingBytes() : reader.ReadBytes(Decode<UNumber>(reader)),
                var t when t == typeof(string) => reader.ReadString(),
                var t when t == typeof(bool) => reader.ReadBoolean(),
                var t when t == typeof(char) => reader.ReadChar(),

                var t when t == typeof(long) => reader.ReadInt64(),
                var t when t == typeof(ulong) => reader.ReadUInt64(),
                var t when t == typeof(int) => reader.ReadInt32(),
                var t when t == typeof(uint) => reader.ReadUInt32(),
                var t when t == typeof(short) => reader.ReadInt16(),
                var t when t == typeof(ushort) => reader.ReadUInt16(),
                var t when t == typeof(byte) => reader.ReadByte(),
                var t when t == typeof(sbyte) => reader.ReadSByte(),

                var t when t == typeof(double) => reader.ReadDouble(),
                var t when t == typeof(float) => reader.ReadSingle(),

                _ => Encoders.TryGetValue(type, out var decoder) ? decoder.Decode(reader) : DecodeUnknown(reader, type)
            };
        }

        private static object DecodeUnknown(BinaryReader _reader, Type _type)
        {
            // Enums
            if (_type.IsEnum)
            {
                return Enum.ToObject(_type, Decode(_reader, _type.GetEnumUnderlyingType()));
            }

            // Arrays
            if (_type.IsArray)
            {
                _type = _type.GetElementType();

                // Unable to read beyond end of stream
                if (_reader.RemainingByteLength() < 2)
                {
                    throw new EndOfStreamException($"Unable to read array. Reached end of stream. {_reader.RemainingByteLength()}");
                }

                var _array = Array.CreateInstance(_type, Decode<UNumber>(_reader));

                for (ushort i = 0; i < _array.Length; i++)
                {
                    _array.SetValue(Decode(_reader, _type), i);
                }

                return _array;
            }

            // Classes and structs
            else
            {
                var _output = Activator.CreateInstance(_type);
                var _manager = PropertyManager.Open(_type);

                for (ushort i = 0; i < _manager.Properties.Length; i++)
                {
                    // Skip properties that have [DontEncode] attribute
                    if (((DontEncode[])_manager.GetInfo(i).GetCustomAttributes(typeof(DontEncode))).Length > 0)
                        continue;

                    LastPropertyName = _manager.GetInfo(i).Name;

                    _manager.SetValue(_output, i, Decode(_reader, _manager.GetType(i)));
                }

                return _output;
            }
        }

        #endregion

        #region Utility [4/4]

        /// <summary>
        /// Safely encodes an object into a byte array
        /// </summary>
        public static bool TryEncode<T>(this T obj, out byte[] buffer, bool enableLogging = true)
        {
            try
            {
                buffer = Encode(obj);
                return buffer != null;
            }

            catch (Exception ex)
            {
                if (enableLogging) Debug.LogError($"Cannot encode typeof({typeof(T)}): {ex.Message}\n{ex.StackTrace}");
                buffer = default;
                return false;
            }
        }

        public static bool TryDecode(this byte[] _buffer, Type _type, out object _value, bool enableLogging = true)
        {
            // Buffer is empty
            if (_buffer.IsEmpty())
            {
                _value = default;
                return false;
            }

            try
            {
                _value = Decode(_buffer, _type);
                return _value != null;
            }

            catch (Exception ex)
            {
                if (enableLogging)
                {
                    switch (ex)
                    {
                        case EndOfStreamException _:
                            Debug.LogError($"Cannot decode as typeof({_type}): Unable to read beyond the end of the stream. Buffer may belong to another data type. [{LastPropertyType.FullName}, {LastPropertyName}?]");
                            Debug.LogWarning($"Error Message: {ex.Message}");
                            break;

                        default:
                            Debug.LogError($"Cannot decode typeof({_type}, {ex.GetType().Name}): {ex.Message}\n{ex.StackTrace}");
                            break;
                    }
                }

                _value = default;
                return false;
            }
        }

        /// <summary>
        /// Safely decodes a byte array into an object
        /// </summary>
        public static bool TryDecode<T>(this byte[] _buffer, out T _value, bool _enableLogging = true)
        {
            var _decoded = TryDecode(_buffer, typeof(T), out var _obj, _enableLogging);

            _value = (T)_obj;
            return _decoded;
        }

        /// <summary>
        /// Returns all remaining bytes in stream of BinaryReader
        /// </summary>
        public static byte[] ReadRemainingBytes(this BinaryReader reader)
        {
            return reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        /// <summary>
        /// Returns length of remaining bytes in stream of BinaryReader
        /// </summary>
        public static long RemainingByteLength(this BinaryReader reader)
        {
            return reader.BaseStream.Length - reader.BaseStream.Position;
        }

        #endregion
    }

    /// <summary>
    /// Defines a non-generic interface for binary encoding
    /// </summary>
    public interface IBinaryEncoder
    {
        void Encode(BinaryWriter writer, ref object value);
        object Decode(BinaryReader reader);
        int Priority();
        bool Active();
    }

    /// <summary>
    /// Defines a generic base class for binary encoding
    /// </summary>
    public class BinaryEncoder<T> : IBinaryEncoder
    {
        public virtual void Encode(BinaryWriter writer, ref object value)
        {

        }

        public virtual object Decode(BinaryReader reader)
        {
            return default;
        }

        public virtual int Priority() => 0;

        public virtual bool Active() => true;
    }
}