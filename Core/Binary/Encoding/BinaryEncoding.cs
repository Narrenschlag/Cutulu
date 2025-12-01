namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Linq;
    using System.IO;
    using System;

    /// <summary>
    /// Static class for encoding and decoding binary data
    /// </summary>
    public static class BinaryEncoding
    {
        #region Register Encoders

        public static readonly Dictionary<nint, IBinaryEncoder> Encoders = [];
        public static int EncoderCount => Encoders?.Count ?? 0;

        public static string LastPropertyName { get; set; }
        public static Type LastPropertyType { get; set; }

        public static readonly Dictionary<Type, GenericBinaryEncoder> GenericEncoderCache = [];
        public static GenericBinaryEncoder[] GenericEncoders { get; private set; }

        static BinaryEncoding()
        {
            RegisterAllEncoders();
        }

        private static void RegisterEncoders()
        {

        }

        private static void RegisterAllEncoders()
        {
            try
            {
                // Get the assembly where BinaryEncoder<T> implementations are located
                var assembly = Assembly.GetExecutingAssembly();
                var flags = Reflection.TypeFinder.DefaultFlags;

                var finder = new Reflection.TypeFinder();
                nint handle;
                Type type;

                // Instantiate each encoder and add it to the dictionary
                finder.FindTypes(type = typeof(BinaryEncoder<>), flags, assembly);
                foreach (var encoderType in finder.Types[type])
                {
                    // Get the generic type argument from the encoder
                    var encoderGenericType = encoderType.BaseType.GetGenericArguments().FirstOrDefault();

                    if (encoderGenericType != null && Activator.CreateInstance(encoderType) is IBinaryEncoder encoderInstance)
                    {
                        // Skip inactive encoders
                        if (encoderInstance.Active() == false) continue;

                        handle = encoderGenericType.TypeHandle.Value;

                        // Assign to the Encoders dictionary
                        if (Encoders.TryGetValue(handle, out var encoder) == false || encoder.Priority() <= encoderInstance.Priority())
                            Encoders[handle] = encoderInstance;
                    }
                }

                // Instantiate each encoder and add it to the dictionary
                var generics = new Dictionary<Type, GenericBinaryEncoder>();

                finder.FindTypes(type = typeof(GenericBinaryEncoder), flags, assembly);
                foreach (var genericType in finder.Types[type])
                {
                    if (Activator.CreateInstance(genericType) is GenericBinaryEncoder encoderInstance)
                    {
                        // Skip inactive encoders
                        if (encoderInstance.Active() == false) continue;

                        var t = encoderInstance.GetType();
                        if (generics.TryGetValue(t, out var existing) == false || existing.Priority() < encoderInstance.Priority())
                            generics[t] = encoderInstance;

                        // Equal priority
                        else if (existing.Priority() == encoderInstance.Priority())
                            Debug.LogR($"[color=darkorange][b][Generic Encoder Setup][/b][/color] {genericType.Name}<{t.Name}> has the same priority as {((object)existing).GetType()}<{t.Name}>: [color=gray]Skipping {genericType.Name}");
                    }
                }

                GenericEncoders = [.. generics.Values];
            }

            catch (Exception ex)
            {
                Debug.LogError($"Failed to register all encoders: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static bool TryGetGenericEncoder(Type type, out GenericBinaryEncoder encoder)
        {
            if (type.IsGenericType == false)
            {
                encoder = null;
                return false;
            }

            // Check cache
            if (GenericEncoderCache.TryGetValue(type, out encoder) == false && GenericEncoders != null)
            {
                // Find encoder
                foreach (var e in GenericEncoders)
                {
                    if (e.ApplysTo(type) == false) continue;

                    GenericEncoderCache[type] = encoder = e;
                    return true;
                }

                // Has no generic encoder
                GenericEncoderCache[type] = null;
            }

            return encoder != null;
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
    public abstract class BinaryEncoder<T> : IBinaryEncoder
    {
        public virtual void Encode(BinaryWriter writer, ref object value) { }
        public virtual object Decode(BinaryReader reader) => default;

        public virtual bool Active() => true;
        public virtual int Priority() => 0;
    }

    public abstract class GenericBinaryEncoder
    {
        public virtual bool ApplysTo(Type type) => false;

        public virtual new Type GetType() => typeof(object);

        public virtual void Encode(BinaryWriter writer, ref object value, Type type) { }
        public virtual object Decode(BinaryReader reader, Type type) => default;

        public virtual bool Active() => true;
        public virtual int Priority() => 0;

        protected static (Type, Type, Func<object, object>, Func<object, object>) CreateMetadata(Type type)
        {
            var args = type.GetGenericArguments();
            var keyProp = type.GetProperty("Key");
            var valueProp = type.GetProperty("Value");

            return (
                args[0],
                args[1],
                CompileGetter(keyProp, type),
                CompileGetter(valueProp, type)
            );
        }

        protected static Func<object, object> CompileGetter(PropertyInfo prop, Type declaringType)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var cast = Expression.Convert(objParam, declaringType);
            var access = Expression.Property(cast, prop);
            var convert = Expression.Convert(access, typeof(object));
            return Expression.Lambda<Func<object, object>>(convert, objParam).Compile();
        }
    }
}