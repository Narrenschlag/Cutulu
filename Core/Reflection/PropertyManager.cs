namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Reflection;
    using System;

    public partial class PropertyManager
    {
        private static readonly Dictionary<Type, Type, PropertyManager> Cache = [];
        public static void ClearCache() => Cache.Clear();

        public readonly Dictionary<string, int> NameToIdx;
        public readonly PropertyInfo[] Properties;
        public readonly Type BaseType;
        public readonly Type Type;

        private PropertyManager(Type type, Type baseType)
        {
            Properties = (Type = type).GetSetProperties();

            // Ignores BaseType properties
            if ((BaseType = baseType) != typeof(object) && type.IsSubclassOf(BaseType))
                Properties = Properties[Open(BaseType).Properties.Length..];

            // Assign NameToIdx
            NameToIdx = [];
            foreach (var property in Properties)
                NameToIdx[property.Name.ToLower()] = NameToIdx.Count;
        }

        ~PropertyManager()
        {
            Cache.Remove(Type, BaseType);
        }

        public static PropertyManager Open<T, B>() where B : T => Open(typeof(T), typeof(B));

        public static PropertyManager Open<T>(Type baseType = null) => Open(typeof(T), baseType);

        /// <summary>
        /// Optimizes the whole process of getting a PropertyManager for a given type by caching it for repeated calls
        /// </summary>
        public static PropertyManager Open(Type type, Type baseType = null)
        {
            if (Cache.TryGetValue(type, baseType ??= typeof(object), out var cached) == false)
                Cache[type, baseType] = cached = new(type, baseType);

            return cached;
        }

        public int GetIndex(string _name) => NameToIdx[PrepareString(_name)];
        public string GetName(int _idx) => Properties[_idx].Name;

        public PropertyInfo GetInfo(int _idx) => Properties[_idx];

        public PropertyInfo GetInfo(string _name) => GetInfo(GetIndex(_name));

        public Type GetType(int _idx) => GetInfo(_idx).PropertyType;

        public Type GetType(string _name) => GetInfo(_name).PropertyType;

        public object GetValue(object _ref, int _idx) => GetInfo(_idx).GetValue(_ref);

        public object GetValue(object _ref, string _name) => GetInfo(_name).GetValue(_ref);

        public void SetValue(object _ref, int _idx, object _value) => GetInfo(_idx).SetValue(_ref, _value);

        public void SetValue(object _ref, string _name, object _value) => GetInfo(_name).SetValue(_ref, _value);

        public static string PrepareString(string _str) => _str.Trim().ToLower();
    }
}