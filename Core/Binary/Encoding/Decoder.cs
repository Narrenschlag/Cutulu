namespace Cutulu.Core
{
    using System.Reflection;
    using System.IO;
    using System;

    /// <summary>
    /// Static class for decoding binary data
    /// </summary>
    public static class Decoder
    {
        /// <summary>
        /// Decodes a buffer from given BinaryReader into an object
        /// </summary>
        public static object Decode(this BinaryReader _reader, Type _type)
        {
            return Decode(_reader, _type, true);
        }

        /// <summary>
        /// Decodes a buffer from given BinaryReader into an object
        /// </summary>
        public static T Decode<T>(this BinaryReader reader)
        {
            return (T)Decode(reader, typeof(T));
        }

        /// <summary>
        /// Decodes a buffer into an object
        /// </summary>
        public static object Decode(this byte[] _buffer, Type _type)
        {
            using var memory = new MemoryStream(_buffer);
            using var reader = new BinaryReader(memory);

            return Decode(reader, _type);
        }

        /// <summary>
        /// Decodes a buffer into an object
        /// </summary>
        public static T Decode<T>(this byte[] _buffer)
        {
            var _obj = Decode(_buffer, typeof(T));
            return _obj is T _t ? _t : default;
        }

        /// <summary>
        /// Safely decodes a buffer into an object
        /// </summary>
        public static bool TryDecode(this BinaryReader _reader, Type _type, out object _value, bool _enable_logging = true)
        {
            try
            {
                return (_value = Decode(_reader, _type)) != null;
            }

            catch (Exception ex)
            {
                if (_enable_logging)
                {
                    switch (ex)
                    {
                        case EndOfStreamException _:
                            Debug.LogError($"Cannot decode as typeof({_type}): Unable to read beyond the end of the stream. Buffer may belong to another data type. [{BinaryEncoding.LastPropertyType.FullName}, {BinaryEncoding.LastPropertyName}?]");
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
        /// Safely decodes a buffer into an object
        /// </summary>
        public static bool TryDecode<T>(this BinaryReader _reader, out T _value, bool _enable_logging = true)
        {
            var _decoded = TryDecode(_reader, typeof(T), out object _obj, _enable_logging);

            _value = (T)_obj;
            return _decoded;
        }

        /// <summary>
        /// Safely decodes a buffer into an object
        /// </summary>
        public static bool TryDecode(this byte[] _buffer, Type _type, out object _value, bool _enable_logging = true)
        {
            try
            {
                return (_value = _buffer.NotEmpty() ? Decode(_buffer, _type) : null) != null;
            }

            catch (Exception ex)
            {
                if (_enable_logging)
                {
                    switch (ex)
                    {
                        case EndOfStreamException _:
                            Debug.LogError($"Cannot decode as typeof({_type}): Unable to read beyond the end of the stream. Buffer may belong to another data type. [{BinaryEncoding.LastPropertyType.FullName}, {BinaryEncoding.LastPropertyName}?]");
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
        /// Safely decodes a buffer into an object
        /// </summary>
        public static bool TryDecode<T>(this byte[] _buffer, out T _value, bool _enable_logging = true)
        {
            var _decoded = TryDecode(_buffer, typeof(T), out var _obj, _enable_logging);

            _value = (T)_obj;
            return _decoded;
        }

        private static object Decode(BinaryReader reader, Type type, bool _first_iteration)
        {
            BinaryEncoding.LastPropertyType = type;

            return type switch
            {
                var t when t == typeof(byte[]) => _first_iteration ? reader.ReadRemainingBytes() : reader.ReadBytes(Decode<UNumber>(reader)),
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

                _ => BinaryEncoding.Encoders.TryGetValue(type, out var decoder) ? decoder.Decode(reader) : DecodeUnknown(reader, type)
            };

            static object DecodeUnknown(BinaryReader _reader, Type _type)
            {
                // Enums
                if (_type.IsEnum)
                {
                    return Enum.ToObject(_type, Decode(_reader, _type.GetEnumUnderlyingType(), false));
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
                        _array.SetValue(Decode(_reader, _type, false), i);
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

                        BinaryEncoding.LastPropertyName = _manager.GetInfo(i).Name;

                        _manager.SetValue(_output, i, Decode(_reader, _manager.GetType(i), false));
                    }

                    return _output;
                }
            }
        }
    }
}