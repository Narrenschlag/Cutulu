namespace Cutulu.Core
{
    using System.Runtime.CompilerServices;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Collections;
    using System.Reflection;
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
            private static readonly ConcurrentDictionary<Type, (Type KeyType, Type ValueType, Func<object, object> GetKey, Func<object, object> GetValue)> Cache = new();

            public override bool ApplysTo(Type type) =>
                type.IsGenericType && type.GetGenericTypeDefinition() == GetType();

            public override Type GetType() => typeof(KeyValuePair<,>);

            public override void Encode(BinaryWriter writer, ref object value, Type type)
            {
                var meta = Cache.GetOrAdd(type, CreateMetadata);

                writer.Encode(meta.GetKey(value), meta.KeyType);
                writer.Encode(meta.GetValue(value), meta.ValueType);
            }

            public override object Decode(BinaryReader reader, Type type)
            {
                var meta = Cache.GetOrAdd(type, CreateMetadata);

                var key = reader.Decode(meta.KeyType);
                var value = reader.Decode(meta.ValueType);

                return Activator.CreateInstance(type, key, value);
            }

            private static (Type, Type, Func<object, object>, Func<object, object>) CreateMetadata(Type type)
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

            private static Func<object, object> CompileGetter(PropertyInfo prop, Type declaringType)
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var cast = Expression.Convert(objParam, declaringType);
                var access = Expression.Property(cast, prop);
                var convert = Expression.Convert(access, typeof(object));
                return Expression.Lambda<Func<object, object>>(convert, objParam).Compile();
            }
        }

        class ICollectionEncoder : GenericBinaryEncoder
        {
            private static readonly ConcurrentDictionary<Type, Type> ItemTypeCache = new();

            public override bool ApplysTo(Type type) =>
                type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == GetType());

            public override Type GetType() => typeof(ICollection<>);

            public override void Encode(BinaryWriter writer, ref object value, Type type)
            {
                var itemType = ItemTypeCache.GetOrAdd(type, t =>
                {
                    return t.GetInterfaces()
                            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
                            .GetGenericArguments()[0];
                });

                var count = new UNumber(value == null ? 0 : ((ICollection)value).Count);
                writer.Encode(count);

                if (count > 0)
                {
                    foreach (var item in (IEnumerable)value)
                        writer.Encode(item, itemType);
                }
            }

            public override object Decode(BinaryReader reader, Type type)
            {
                var itemType = ItemTypeCache.GetOrAdd(type, t =>
                {
                    return t.GetInterfaces()
                            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
                            .GetGenericArguments()[0];
                });

                var count = reader.Decode<UNumber>();
                var listType = typeof(List<>).MakeGenericType(itemType);
                var list = (IList)Activator.CreateInstance(listType);

                for (int i = 0; i < count; i++)
                    list.Add(reader.Decode(itemType));

                if (type.IsAssignableFrom(listType))
                    return list;

                return Activator.CreateInstance(type, list);
            }
        }

        class TupleEncoder : GenericBinaryEncoder
        {
            public override bool ApplysTo(Type type) =>
                type.IsGenericType && GetType().IsAssignableFrom(type);

            public override Type GetType() => typeof(ITuple);

            public override void Encode(BinaryWriter writer, ref object value, Type type)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "Tuple value is null.");

                var (argTypes, getters) = TupleEncoderCache.Cache.GetOrAdd(type, CreateMetadata);

                for (int i = 0; i < argTypes.Length; i++)
                {
                    var item = getters[i]?.Invoke(value); // safe invoke
                    writer.Encode(item, argTypes[i]);
                }
            }

            public override object Decode(BinaryReader reader, Type type)
            {
                var (argTypes, _) = TupleEncoderCache.Cache.GetOrAdd(type, CreateMetadata);

                var args = new object[argTypes.Length];
                for (int i = 0; i < argTypes.Length; i++)
                    args[i] = reader.Decode(argTypes[i]);

                return Activator.CreateInstance(type, args);
            }

            private static (Type[] ArgTypes, Func<object, object>[] Getters) CreateMetadata(Type type)
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                                 .Where(f => f.Name.StartsWith("Item"))
                                 .OrderBy(f => f.Name)
                                 .ToArray();

                if (fields.Length == 0)
                    throw new InvalidOperationException($"No ItemN fields found in type {type.FullName}");

                var argTypes = new Type[fields.Length];
                var getters = new Func<object, object>[fields.Length];

                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    argTypes[i] = field.FieldType;

                    var objParam = Expression.Parameter(typeof(object), "obj");
                    var castObj = Expression.Convert(objParam, type);
                    var access = Expression.Field(castObj, field);
                    var convert = Expression.Convert(access, typeof(object));
                    getters[i] = Expression.Lambda<Func<object, object>>(convert, objParam).Compile();
                }

                return (argTypes, getters);
            }

            static class TupleEncoderCache
            {
                public static readonly ConcurrentDictionary<Type, (Type[] ArgTypes, Func<object, object>[] Getters)> Cache = new();
            }
        }
    }
}