namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Collections;
    using System.Linq;
    using System.IO;
    using System;

    /// <summary>
    /// Use static method Register() to see more.
    /// </summary>
    public static class AdvancedEncoders
    {
        class KeyValuePairEncoder : GenericBinaryEncoder
        {
            public override bool ApplysTo(Type type) => type.GetGenericTypeDefinition() == GetType();
            public override Type GetType() => typeof(KeyValuePair<,>);

            public override void Encode(BinaryWriter writer, ref object value, Type type)
            {
                if (type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                    throw new InvalidOperationException($"Not a KeyValuePair. [{type.Name}]");

                writer.Encode(type.GetProperty("Key").GetValue(value), type.GetGenericArguments()[0]);
                writer.Encode(type.GetProperty("Value").GetValue(value), type.GetGenericArguments()[1]);
            }

            public override object Decode(BinaryReader reader, Type type)
            {
                if (type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                    throw new InvalidOperationException($"Not a KeyValuePair. [{type.Name}]");

                var typeArgs = type.GetGenericArguments();

                return Activator.CreateInstance(type, reader.Decode(typeArgs[0]), reader.Decode(typeArgs[1]));
            }
        }

        class ICollectionEncoder : GenericBinaryEncoder
        {
            public override bool ApplysTo(Type type) => type.GetInterfaces().Any(i => i.IsGenericType &&
                i.GetGenericTypeDefinition() == GetType());

            public override Type GetType() => typeof(ICollection<>);

            public override void Encode(BinaryWriter writer, ref object value, Type type)
            {
                // Get the actual ICollection<T> interface implemented by this type
                var iCollectionInterface = type
                    .GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)) ??
                    throw new InvalidOperationException("Type does not implement ICollection<T>");

                writer.Encode(new UNumber((int)iCollectionInterface.GetProperty("Count").GetValue(value)));
                var itemType = iCollectionInterface.GetGenericArguments()[0];

                foreach (var item in (IEnumerable)value)
                    writer.Encode(item, itemType);
            }

            public override object Decode(BinaryReader reader, Type type)
            {
                // Get the actual ICollection<T> interface implemented by this type
                var iCollectionInterface = type
                    .GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)) ??
                    throw new InvalidOperationException("Type does not implement ICollection<T>");

                var count = reader.Decode<UNumber>();

                // Create List<T> for population
                var itemType = iCollectionInterface.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(itemType);
                var list = (IList)Activator.CreateInstance(listType);

                for (int i = 0; i < count; i++)
                    list.Add(reader.Decode(itemType));

                // If target type is not List<T>, try to convert
                if (type.IsAssignableFrom(listType)) return list;

                // Try to construct the target type from IEnumerable<T>
                return Activator.CreateInstance(type, list);
            }
        }
    }
}