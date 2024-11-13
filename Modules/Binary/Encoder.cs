namespace Cutulu
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

        private static readonly Dictionary<Type, IBinaryEncoder> Encoders = new();
        public static int EncoderCount => Encoders.Count;

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
            return memory.ToArray() ?? Array.Empty<byte>();
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
                case byte[] v when raw: writer.Write(v); break;

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
                    if (Encoders.TryGetValue(obj.GetType(), out var encoder)) encoder.Encode(writer, ref obj);
                    else if (EncodeUnknown(writer, ref obj) == false) return false;
                    break;
            }

            return true;
        }

        private static bool EncodeUnknown(BinaryWriter writer, ref object obj)
        {
            if (obj == null) return false;
            var type = obj.GetType();

            // Arrays
            if (type.IsArray && obj is Array array)
            {
                writer.Write((ushort)array.Length);
                type = type.GetElementType();

                for (ushort i = 0; i < array.Length; i++)
                {
                    var value = array.GetValue(i);

                    // Write null array as empty array
                    if (value == null && type.IsArray) writer.Write(default(ushort));

                    // Write array value
                    else writer.Write(Encode(value));
                }
            }

            // Classes and structs
            else
            {
                var properties = type.GetProperties();

                for (ushort i = 0; i < properties.Length; i++)
                {
                    var value = properties[i].GetValue(obj);
                    type = properties[i].GetType();

                    // Write null array as empty array
                    if (value == null && type.IsArray) writer.Write(default(ushort));

                    // Write value
                    else writer.Write(Encode(value));
                }
            }

            return true;
        }

        #endregion

        #region Output [3/4]

        /// <summary>
        /// Decodes a byte array into an object
        /// </summary>
        public static T Decode<T>(this byte[] buffer)
        {
            using var memory = new MemoryStream(buffer);
            using var reader = new BinaryReader(memory);

            return (T)Decode(reader, typeof(T));
        }

        /// <summary>
        /// Decodes a byte array from the BinaryReader into an object
        /// </summary>
        public static T Decode<T>(this BinaryReader reader)
        {
            return (T)Decode(reader, typeof(T));
        }

        private static object Decode(this BinaryReader reader, Type type)
        {
            return type switch
            {
                var t when t == typeof(byte[]) => reader.ReadRemainingBytes(),
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

        private static object DecodeUnknown(BinaryReader reader, Type type)
        {
            // Arrays
            if (type.IsArray)
            {
                type = type.GetElementType();

                // Unable to read beyond end of stream
                if (reader.RemainingByteLength() < 2)
                {
                    throw new EndOfStreamException($"Unable to read array. Reached end of stream.");
                }

                var array = Array.CreateInstance(type, reader.ReadUInt16());

                for (ushort i = 0; i < array.Length; i++)
                {
                    array.SetValue(Decode(reader, type), i);
                }

                return array;
            }

            // Classes and structs
            else
            {
                var output = Activator.CreateInstance(type);
                var properties = type.GetProperties();

                for (ushort i = 0; i < properties.Length; i++)
                {
                    properties[i].SetValue(output, Decode(reader, properties[i].PropertyType));
                }

                return output;
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

        /// <summary>
        /// Safely decodes a byte array into an object
        /// </summary>
        public static bool TryDecode<T>(this byte[] buffer, out T value, bool enableLogging = true)
        {
            // Buffer is empty
            if (buffer.IsEmpty())
            {
                value = default;
                return false;
            }

            try
            {
                value = Decode<T>(buffer);
                return value != null;
            }

            catch (Exception ex)
            {
                if (enableLogging) Debug.LogError($"Cannot decode typeof({typeof(T)}): {ex.Message}\n{ex.StackTrace}");
                value = default;
                return false;
            }
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