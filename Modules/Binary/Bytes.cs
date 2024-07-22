using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;

namespace Cutulu
{
    /// <summary>
    /// Used for writing values into bytes and reading values from bytes.
    /// <br/>Allows for infinite recursion and internal classes.
    /// <br/>Keep in mind to use { get; set; }.
    /// </summary>
    public static class Bytes
    {
        #region Additional Formatters   ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static Dictionary<Type, ByteFormatter> additionalFormatters;
        public static Dictionary<Type, ByteFormatter> AdditionalFormatters
        {
            get
            {
                // Bootstrap
                if (additionalFormatters == null)
                {
                    additionalFormatters = new();

                    CutuluByteFormatters.Register();
                    GodotByteFormatters.Register();
                }

                return additionalFormatters;
            }
        }

        /// <summary>
        /// Registers a formatter for non primitive data structures
        /// </summary>
        public static void RegisterFormatter(Type type, ByteFormatter formatter, bool overrideExisting = true)
        {
            // Formatter invalid
            if (formatter == null)
            {
                return;
            }

            // Override existing
            else if (AdditionalFormatters.ContainsKey(type))
            {
                if (overrideExisting)
                {
                    AdditionalFormatters[type] = formatter;
                }
            }

            // Add new
            else
            {
                AdditionalFormatters.Add(type, formatter);
            }
        }

        /// <summary>
        /// Removes formatter for non primitive data structures
        /// </summary>
        public static void ClearFormatter(Type type) => AdditionalFormatters.TryRemove(type);

        /// <summary>
        /// Tries to find custom formatter for non primitive data structure
        /// </summary>
        private static bool TryGetFormatter(Type type, out ByteFormatter formatter) => AdditionalFormatters.TryGetValue(type, out formatter);
        #endregion

        #region Utility                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static bool IsDeserializableValue(ref Type type) => type.IsPrimitive || type == typeof(string) || (AdditionalFormatters != null && AdditionalFormatters.ContainsKey(type));
        private static bool SerializeAsContainer(ref Type type) => IsDeserializableValue(ref type) == false && type != typeof(string);

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

        /// <summary>
        /// Gets the number of bytes remaining in the BinaryReader's underlying stream.
        /// </summary>
        /// <param name="reader">The BinaryReader to check.</param>
        /// <returns>The number of bytes remaining in the stream.</returns>
        public static long BytesRemaining(this BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            // Cast the underlying stream to MemoryStream to get its Length property
            if (reader.BaseStream is MemoryStream memoryStream)
            {
                return memoryStream.Length - memoryStream.Position;
            }

            // For other types of streams, you may not be able to determine the length
            throw new InvalidOperationException("Cannot determine the number of remaining bytes for this type of stream.");
        }
        #endregion

        #region To Bytes                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Turns everything into bytes.
        /// <br/>Only writes values declared as { public get; } and/or { public set; }.
        /// </summary>
        public static byte[] Buffer(this object value)
        {
            if (value == null) return null;

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            ToBytes(value.GetType(), ref value, writer);

            byte[] bytes = stream.ToArray();
            stream.Close();
            writer.Close();

            return bytes;
        }

        private static void ToBytes(Type type, ref object value, BinaryWriter writer)
        {
            // Arrays
            if (type.IsArray && value is Array array)
            {
                writer.Write((ushort)array.Length);
                type = type.GetElementType();

                for (ushort i = 0; i < array.Length; i++)
                {
                    object element = array.GetValue(i);

                    ToBytes(type, ref element, writer);
                }
            }

            // Classes and structs
            else if (SerializeAsContainer(ref type))
            {
                PropertyInfo[] properties = type.GetProperties();
                for (ushort i = 0; i < properties.Length; i++)
                {
                    object _value = properties[i].GetValue(value);

                    ToBytes(_value.GetType(), ref _value, writer);
                }
            }

            // Everything else
            else
            {
                // Write based on type
                switch (value)
                {
                    // Text
                    case string v: writer.Write(v); break;
                    case char v: writer.Write(v); break;

                    // Integers
                    case uint v: writer.Write(v); break;
                    case int v: writer.Write(v); break;

                    // Shorts
                    case ushort v: writer.Write(v); break;
                    case short v: writer.Write(v); break;

                    // Bytes and bools
                    case byte v: writer.Write(v); break;
                    case sbyte v: writer.Write(v); break;
                    case bool v: writer.Write(v); break;

                    // Floats
                    case double v: writer.Write(v); break;
                    case float v: writer.Write(v); break;

                    // Custom or non-supported
                    default:
                        // Custom formatting for registered types
                        if (TryGetFormatter(type, out ByteFormatter formatter))
                        {
                            formatter.Write(value, writer);
                            break;
                        }

                        Debug.LogError($"typeof({type}) is not supported and gets thereby skipped.\nYou could add an additional ByteFormatterOverride and register it.");
                        break;
                };
            }
        }
        #endregion

        #region From Bytes              ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Tries turning bytes into any value.
        /// <br/>Only reads values declared as { public get; } and/or { public set; }.
        /// </summary>
        public static bool TryBuffer<T>(this byte[] bytes, out T output)
        {
            try
            {
                output = Buffer<T>(bytes, out var failed);
                return failed == false;
            }

            catch
            {
                output = default;
                return false;
            }
        }

        /// <summary>
        /// Tries reading generic value from BinaryReader.
        /// <br/>Only reads values that can be buffered and not strings.
        /// </summary>
        public static bool TryBuffer<T>(this BinaryReader reader, out T output)
        {
            try
            {
                if (typeof(T).Equals(typeof(string)))
                {
                    output = (T)(object)reader.ReadString();

                    return true;
                }

                else
                {
                    var buffer = reader.ReadBytes(GetSizeOf<T>());
                    output = Buffer<T>(buffer, out var failed);

                    return failed == false;
                }
            }

            catch
            {
                output = default;
                return false;
            }
        }

        /// <summary>
        /// Turns bytes into any value.
        /// <br/>Only reads values declared as { public get; } and/or { public set; }.
        /// </summary>
        public static T Buffer<T>(this byte[] bytes) => Buffer<T>(bytes, out _);

        /// <summary>
        /// Turns bytes into any value.
        /// <br/>Only reads values declared as { public get; } and/or { public set; }.
        /// </summary>
        public static T Buffer<T>(this byte[] bytes, out bool failed)
        {
            // Empty byte buffer
            if (bytes == null || bytes.Length < 1)
            {
                failed = true;

                Debug.LogError("Cannot read an empty byte buffer.");
                return default;
            }

            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            FromBytes(typeof(T), out object output, reader);

            stream.Close();
            reader.Close();

            failed = false;
            return output != null && output is T t ? t : default;
        }

        private static void FromBytes(Type type, out object output, BinaryReader reader)
        {
            // Arrays
            if (type.IsArray)
            {
                ushort length = reader.ReadUInt16();
                type = type.GetElementType();

                Array array = Array.CreateInstance(type, length);
                for (ushort i = 0; i < length; i++)
                {
                    FromBytes(type, out object element, reader);
                    array.SetValue(element, i);
                }

                output = array;
            }

            // Classes and structs
            else if (SerializeAsContainer(ref type))
            {
                PropertyInfo[] properties = type.GetProperties();
                output = Activator.CreateInstance(type);

                for (ushort i = 0; i < properties.Length; i++)
                {
                    FromBytes(properties[i].PropertyType, out var value, reader);
                    properties[i].SetValue(output, value);
                }
            }

            // Everything else
            else
            {
                output = type switch
                {
                    // Text
                    var t when t == typeof(string) => reader.ReadString(),
                    var t when t == typeof(char) => reader.ReadChar(),

                    // Ints
                    var t when t == typeof(uint) => reader.ReadUInt32(),
                    var t when t == typeof(int) => reader.ReadInt32(),

                    // Shorts
                    var t when t == typeof(ushort) => reader.ReadUInt16(),
                    var t when t == typeof(short) => reader.ReadInt16(),

                    // Bytes and bools
                    var t when t == typeof(byte) => reader.ReadByte(),
                    var t when t == typeof(sbyte) => reader.ReadSByte(),
                    var t when t == typeof(bool) => reader.ReadBoolean(),

                    // Floats
                    var t when t == typeof(double) => reader.ReadDouble(),
                    var t when t == typeof(float) => reader.ReadSingle(),

                    // Custom or non-supported
                    _ => custom()
                };

                // Custom or non-supported
                object custom()
                {
                    // Custom formatting for registered types
                    if (TryGetFormatter(type, out ByteFormatter formatter))
                    {
                        return formatter.Read(reader);
                    }

                    Debug.LogError($"typeof({type}) is not supported and gets thereby skipped.");
                    return default;
                }
            }
        }
        #endregion
    }

    #region Byte Formatter              ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Used to create custom formatters for non primitive data structures
    /// </summary>
    public class ByteFormatter
    {
        /// <summary>
        /// Defines logic to write data structure into bytes
        /// </summary>
        public virtual void Write(object value, BinaryWriter writer) { }

        /// <summary>
        /// Defines logic to read data structure from bytes
        /// </summary>
        public virtual object Read(BinaryReader reader) => default;

        /// <summary>
        /// Registers this formatter to the global registry with the type associated
        /// </summary>
        public void Register<TargetType>(bool overrideExisting = true)
        => Bytes.RegisterFormatter(typeof(TargetType), this, overrideExisting);
    }

    /// <summary>
    /// Used to create custom generic formatters for non primitive data structures
    /// </summary>
    public class GenericByteFormatter<T> : ByteFormatter
    {
        public void Register() => Register<T>();
    }
    #endregion

    #region BinaryReader Extension      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Extends the binary read for generics
    /// </summary>
    public static class BinaryReaderExtensions
    {
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
    }
    #endregion
}