using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;

namespace Cutulu
{
    /// <summary>
    /// Used for writing values into bytes and reading values from bytes.
    /// Allows for infinite recursion and internal classes.
    /// Keep in mind to use { get; set; }.
    /// </summary>
    public static class Bytes
    {
        #region Additional Formatters   ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static Dictionary<Type, ByteFormatter> AdditionalFormatters;

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

            // Setup instances
            else if (AdditionalFormatters == null)
            {
                AdditionalFormatters = new(){
                { type, formatter }
            };
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
        public static void ClearFormatter(Type type)
        {
            if (AdditionalFormatters != null && AdditionalFormatters.ContainsKey(type))
            {
                AdditionalFormatters.Remove(type);
            }
        }

        /// <summary>
        /// Tries to find custom formatter for non primitive data structure
        /// </summary>
        private static bool TryGetFormatter(Type type, out ByteFormatter formatter)
        {
            if (AdditionalFormatters == null)
            {
                formatter = null;
                return false;
            }

            else
            {
                return AdditionalFormatters.TryGetValue(type, out formatter);
            }
        }
        #endregion

        #region Utility                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static bool IsDeserializableValue(ref Type type) => type.IsPrimitive || type == typeof(string) || (AdditionalFormatters != null && AdditionalFormatters.ContainsKey(type));
        private static bool SerializeAsContainer(ref Type type) => IsDeserializableValue(ref type) == false && type != typeof(string);
        #endregion

        #region To Bytes                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Turns everything into bytes.
        /// <br/>Only writes values declared as { public get; } and/or { public set; }.
        /// </summary>
        public static byte[] Buffer(this object value)
        {
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
                switch (type)
                {
                    // Text
                    case var t when t == typeof(string): writer.Write((string)value); break;
                    case var t when t == typeof(char): writer.Write((char)value); break;

                    // Integers
                    case var t when t == typeof(uint): writer.Write((uint)value); break;
                    case var t when t == typeof(int): writer.Write((int)value); break;

                    // Shorts
                    case var t when t == typeof(ushort): writer.Write((ushort)value); break;
                    case var t when t == typeof(short): writer.Write((ushort)value); break;

                    // Bytes and bools
                    case var t when t == typeof(byte): writer.Write((byte)value); break;
                    case var t when t == typeof(bool): writer.Write((bool)value); break;

                    // Floats
                    case var t when t == typeof(double): writer.Write((double)value); break;
                    case var t when t == typeof(float): writer.Write((float)value); break;

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
                output = Buffer<T>(bytes, out bool failed);
                return failed == false;
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
                    FromBytes(properties[i].PropertyType, out object value, reader);
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
    #endregion
}