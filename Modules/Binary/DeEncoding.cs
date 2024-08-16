using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System;

namespace Cutulu
{
    public static class DeEncoding
    {
        #region Encoders [1/3]
        private static readonly Dictionary<Type, IBinaryEncoder> Encoders = new();
        public static int EncoderCount => Encoders.Count;

        static DeEncoding()
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

        #region Encode
        public static byte[] Encode(this object obj)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);

            Encode(writer, ref obj);

            return memory.ToArray();
        }

        public static void Encode(BinaryWriter writer, ref object obj)
        {
            if (obj == null) return;

            switch (obj)
            {
                case byte[] v: writer.Write(v); break;

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
                    if (Encoders.TryGetValue(obj.GetType(), out var encoder))
                        encoder.Encode(writer, ref obj);
                    else
                        writer.Write(EncodeUnknown(ref obj) ?? Array.Empty<byte>());
                    break;
            }
        }

        private static byte[] EncodeUnknown(ref object obj)
        {
            var type = obj.GetType();
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);

            // Arrays
            if (type.IsArray && obj is Array array)
            {
                writer.Write((ushort)array.Length);
                type = type.GetElementType();

                for (ushort i = 0; i < array.Length; i++)
                {
                    writer.Write(Encode(array.GetValue(i)));
                }
            }

            // Classes and structs
            else
            {
                var properties = type.GetProperties();

                for (ushort i = 0; i < properties.Length; i++)
                {
                    writer.Write(Encode(properties[i].GetValue(obj)));
                }
            }

            return memory.ToArray();
        }
        #endregion

        #region Decode
        public static bool TryDecode<T>(this byte[] buffer, out T value)
        {
            try
            {
                value = Decode<T>(buffer);
            }

            catch (Exception ex)
            {
                Debug.LogError($"Cannot decode typeof({typeof(T)}): {ex.Message}\n{ex.StackTrace}");
                value = default;
            }

            return value != null;
        }

        public static T Decode<T>(this byte[] buffer)
        {
            return Decode(buffer, typeof(T)) is T t ? t : default;
        }

        public static bool TryDecode(this byte[] buffer, Type type, out object value)
        {
            try
            {
                value = Decode(buffer, type);
            }

            catch (Exception ex)
            {
                Debug.LogError($"Cannot decode typeof({type}): {ex.Message}\n{ex.StackTrace}");
                value = default;
            }

            return value != null;
        }

        public static object Decode(this byte[] buffer, Type type)
        {
            using var memory = new MemoryStream(buffer);
            using var reader = new BinaryReader(memory);

            return Decode(reader, type);
        }

        public static bool TryDecode<T>(this BinaryReader reader, out T value)
        {
            if (TryDecode(reader, typeof(T), out var obj) && obj is T val)
            {
                value = val;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryDecode(this BinaryReader reader, Type type, out object value)
        {
            try
            {
                value = Decode(reader, type);
            }

            catch (Exception ex)
            {
                Debug.LogError($"Cannot decode typeof({type}): {ex.Message}\n{ex.StackTrace}");
                value = default;
            }

            return value != null;
        }

        public static object Decode(this BinaryReader reader, Type type)
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
                ushort length = reader.ReadUInt16();
                type = type.GetElementType();

                var array = Array.CreateInstance(type, length);
                for (ushort i = 0; i < length; i++)
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

        #region Utility
        /// <summary>
        /// Returns all remaining bytes in stream of BinaryReader
        /// </summary>
        public static byte[] ReadRemainingBytes(this BinaryReader reader)
        {
            return reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        /// <summary>
        /// Uses the Marshal to allocate data from binary reader and cast it into generic Type : struct
        /// </summary>
        public static T ReadViaMarshal<T>(this BinaryReader reader) where T : struct
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var size = Marshal.SizeOf(typeof(T));
            var bytes = reader.ReadBytes(size);

            if (bytes.Length != size)
                throw new EndOfStreamException("Could not read enough bytes for the specified type.");

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                var pointer = handle.AddrOfPinnedObject();
                return (T)Marshal.PtrToStructure(pointer, typeof(T));
            }

            finally
            {
                handle.Free();
            }
        }

        public static int GetExpectedSizeOf<T>(this T obj) => GetSizeOf<T>();
        public static int GetSizeOf<T>()
        {
            if (typeof(T).Equals(typeof(string)))
            {
                Debug.LogError($"typeof(string) is not supported for GetSizeOf() as it has a generic length.");
                return 0;
            }

            return Marshal.SizeOf(typeof(T));
        }
        #endregion
    }

    #region Encoders [2/3]
    // Define a non-generic interface for binary encoding
    public interface IBinaryEncoder
    {
        void Encode(BinaryWriter writer, ref object value);
        object Decode(BinaryReader reader);
        int Priority();
        bool Active();
    }

    // Generic implementation of the base interface
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
    #endregion
}