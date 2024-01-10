using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;

namespace Cutulu
{
    public static class ByteFormatting
    {
        #region Additional Formatters
        private static Dictionary<Type, ByteFormatter> AdditionalFormatters;
        public static void RegisterFormatter(Type type, ByteFormatter formatter, bool overrideExisting = true)
        {
            if (AdditionalFormatters == null)
            {
                AdditionalFormatters = new(){
                { type, formatter }
            };
            }

            else if (AdditionalFormatters.ContainsKey(type))
            {
                if (overrideExisting)
                {
                    AdditionalFormatters[type] = formatter;
                }
            }

            else
            {
                AdditionalFormatters.Add(type, formatter);
            }
        }

        public static void ClearFormatter(Type type)
        {
            if (AdditionalFormatters != null && AdditionalFormatters.ContainsKey(type))
            {
                AdditionalFormatters.Remove(type);
            }
        }

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

        #region Serialize
        // For classes/struct
        public static byte[] Serialize<T>(this T source) where T : new()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            object value = default;
            Type type;

            for (int i = 0; i < properties.Length; i++)
            {
                value = properties[i].GetValue(source);

                if (Write(type = properties[i].PropertyType, ref value, writer) == false)
                {
                    $"Value Type of {type} is not supported".LogError();
                    continue;
                }
            }

            stream.Close();
            writer.Close();
            return stream.ToArray();
        }

        // For values
        public static byte[] SerializeValue<T>(this T value)
        {
            using MemoryStream stream = new();

            using BinaryWriter writer = new BinaryWriter(stream);
            object obj = value;

            return Write(value.GetType(), ref obj, writer) ? stream.ToArray() : null;
        }

        // Secret backend
        private static bool Write(Type type, ref object value, BinaryWriter writer)
        {
            #region Array
            if (type.IsArray && value is Array array)
            {
                using MemoryStream _stream = new();
                using BinaryWriter _writer = new(_stream);

                ushort length = (ushort)array.Length;
                _writer.Write(length);

                for (ushort i = 0; i < length; i++)
                {
                    object obj = array.GetValue(i);

                    if (Write(obj.GetType(), ref obj, _writer) == false)
                    {
                        _stream.Close();
                        _writer.Close();
                        return false;
                    }
                }

                writer.Write(_stream.ToArray());

                _stream.Close();
                _writer.Close();
                return true;
            }
            #endregion

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
                    if (TryGetFormatter(type, out ByteFormatter formatter))
                    {
                        formatter.Write(value, writer);
                        break;
                    }

                    return false;
            };

            return true;
        }
        #endregion

        #region Deserialize
        // For classes and structs
        public static T Deserialize<T>(this byte[] bytes) where T : new()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            object value = default;
            T result = new();
            Type type;

            for (int i = 0; i < properties.Length; i++)
            {
                // Return if there are no bytes left and it's therefore a type mismatch
                if (bytes.Length < 1)
                {
                    "Class/Struct Type mismatch".LogError();

                    stream.Close();
                    reader.Close();
                    return default;
                }

                // Ignore value of it's not supported by any built in or addition formatter
                if (Read(type = properties[i].PropertyType, out value, reader) == false)
                {
                    $"Value Type of {type} is not supported. This may produce further problems.".LogError();
                    continue;
                }

                // Types are not lining up and therefore the cast is invalid
                if (type != value.GetType())
                {
                    $"Value Type mismatch required({type}) != result({value.GetType()})".LogError();

                    stream.Close();
                    reader.Close();
                    return default;
                }

                // Apply value
                properties[i].SetValue(result, value);
            }

            stream.Close();
            reader.Close();
            return result;
        }

        // For values
        public static T DeserializeValue<T>(this byte[] bytes)
        {
            using MemoryStream stream = new(bytes);

            using BinaryReader reader = new BinaryReader(stream);

            return Read(typeof(T), out object result, reader) ? (T)result : default;
        }

        private static bool Read(Type type, out object value, BinaryReader reader)
        {
            #region Array
            if (type.IsArray)
            {
                ushort length = reader.ReadUInt16();
                Type _type = type.GetElementType();

                Array array = Array.CreateInstance(_type, length);
                for (ushort i = 0; i < length; i++)
                    if (Read(_type, out object _v, reader))
                    {
                        array.SetValue(_v, i);
                    }

                    else
                    {
                        value = default;
                        return false;
                    }

                value = array;
                return true;
            }
            #endregion

            value = type switch
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
                if (TryGetFormatter(type, out ByteFormatter formatter))
                {
                    return formatter.Read(reader);
                }

                return new Exception();
            }

            return value.GetType() != typeof(Exception);
        }
        #endregion
    }

    public class ByteFormatter
    {
        public virtual void Write(object value, BinaryWriter writer) { }
        public virtual object Read(BinaryReader reader) => default;

        /// <summary> Adds this formatter to the global registry </summary>
        public void Register<T>(bool overrideExisting = true)
        => ByteFormatting.RegisterFormatter(typeof(T), this, overrideExisting);
    }
}