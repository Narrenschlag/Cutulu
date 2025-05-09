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

        static BinaryEncoding()
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