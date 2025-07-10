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
    public static class BinaryEncoding
    {
        #region Register Encoders

        public static readonly Dictionary<Type, IBinaryEncoder> Encoders = [];
        public static int EncoderCount => Encoders.Count;

        public static string LastPropertyName { get; set; }
        public static Type LastPropertyType { get; set; }

        public static readonly Dictionary<Type, GenericBinaryEncoder> GenericEncoderCache = [];
        public static GenericBinaryEncoder[] GenericEncoders { get; private set; }

        static BinaryEncoding()
        {
            RegisterAllEncoders();
        }

        private static void RegisterAllEncoders()
        {
            // Get the assembly where BinaryEncoder<T> implementations are located
            var assembly = Assembly.GetExecutingAssembly();
            var flags = Reflection.TypeFinder.DefaultFlags;

            var finder = new Reflection.TypeFinder();
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

                    // Assign to the Encoders dictionary
                    if (Encoders.TryGetValue(encoderGenericType, out var encoder) == false || encoder.Priority() <= encoderInstance.Priority())
                        Encoders[encoderGenericType] = encoderInstance;
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
    }
}