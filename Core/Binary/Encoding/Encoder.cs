namespace Cutulu.Core
{
    using System.Reflection;
    using System.IO;
    using System;

    /// <summary>
    /// Static class for encoding and decoding binary data
    /// </summary>
    public static class Encoder
    {
        /// <summary>
        /// Writes encoded buffer of an object to given BinaryWriter
        /// </summary>
        public static void Encode(this BinaryWriter _writer, object _obj, Type _type)
        {
            // Write empty array
            if (_obj.IsNull() && _type.IsArray) _writer.Write(new UNumber());

            // Write object
            else Encode(_writer, _obj, true);
        }

        /// <summary>
        /// Writes encoded buffer of an object to given BinaryWriter
        /// </summary>
        public static void Encode<T>(this BinaryWriter _writer, T _obj)
        {
            Encode(_writer, _obj, typeof(T));
        }

        /// <summary>
        /// Encodes an object into a buffer
        /// </summary>
        public static byte[] Encode(this object obj, Type _type)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);

            Encode(writer, obj, _type);

            return memory.ToArray();
        }

        /// <summary>
        /// Encodes an object into a buffer
        /// </summary>
        public static byte[] Encode<T>(this T obj)
        {
            return Encode(obj, typeof(T));
        }

        /// <summary>
        /// Safely encodes an object into a buffer
        /// </summary>
        public static bool TryEncode(this BinaryWriter _writer, object _obj, Type _type, bool _enable_logging = true)
        {
            try
            {
                Encode(_writer, _obj, _type);
                return true;
            }

            catch (Exception ex)
            {
                if (_enable_logging) Debug.LogError($"Cannot encode typeof({_type}): {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Safely encodes an object into a buffer
        /// </summary>
        public static bool TryEncode<T>(this BinaryWriter _writer, T _obj, bool _enable_logging = true)
        {
            return TryEncode(_writer, _obj, typeof(T), _enable_logging);
        }

        /// <summary>
        /// Safely encodes an object into a buffer
        /// </summary>
        public static bool TryEncode(object _obj, Type _type, out byte[] _buffer, bool _enable_logging = true)
        {
            try
            {
                return (_buffer = Encode(_obj, _type)) != null;
            }

            catch (Exception ex)
            {
                if (_enable_logging) Debug.LogError($"Cannot encode typeof({_type}): {ex.Message}\n{ex.StackTrace}");
                _buffer = null;
                return false;
            }
        }

        /// <summary>
        /// Safely encodes an object into a buffer
        /// </summary>
        public static bool TryEncode<T>(T _obj, out byte[] _buffer, bool _enable_logging = true)
        {
            return TryEncode(_obj, typeof(T), out _buffer, _enable_logging);
        }

        private static bool Encode(BinaryWriter writer, object obj, bool _first_iteration)
        {
            if (obj.IsNull()) return false;

            switch (obj)
            {
                case byte[] v:
                    if (_first_iteration == false) Encode(writer, new UNumber(v.Length), false);
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
                    var type = obj.GetType();

                    // Encode using custom encoder
                    if (BinaryEncoding.Encoders.TryGetValue(type, out var encoder)) encoder.Encode(writer, ref obj);

                    // Encode using custom generic encoder
                    else if (BinaryEncoding.TryGetGenericEncoder(type, out var genericEncoder)) genericEncoder.Encode(writer, ref obj, type);

                    // Encode types without serializer
                    else if (EncodeUnknown(writer, ref obj) == false) return false;

                    break;
            }

            return true;

            static bool EncodeUnknown(BinaryWriter _writer, ref object _obj)
            {
                //if (_obj == null) return false; -> _obj is already null-checked
                var _type = _obj.GetType();

                // Encode enum
                if (_type.IsEnum)
                {
                    Encode(_writer, Convert.ChangeType(_obj, _type.GetEnumUnderlyingType()), false);
                }

                // Arrays
                else if (_type.IsArray && _obj is Array array)
                {
                    Encode(_writer, new UNumber(array.Length), false);
                    _type = _type.GetElementType();

                    for (int i = 0; i < array.Length; i++)
                    {
                        var _value = array.GetValue(i);

                        // Write null array as empty array
                        if (_value == null && _type.IsArray) _writer.Write(new UNumber());

                        // Write array value
                        else Encode(_writer, _value, false);
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
                        else Encode(_writer, _value, false); // Encode value as usual
                    }
                }

                return true;
            }
        }
    }
}