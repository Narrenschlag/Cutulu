namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Reflection;
    using System;

    public partial class PropertyManager
    {
        private static readonly Dictionary<Type, PropertyManager> Cache = [];
        public static void ClearCache() => Cache.Clear();

        public readonly Dictionary<string, int> NameToIdx;
        public readonly PropertyInfo[] Properties;
        public readonly Type Type;

        public PropertyManager(Type _type)
        {
            Properties = (Type = _type).GetSetProperties();

            NameToIdx = [];
            foreach (var _property in Properties)
                NameToIdx[_property.Name.ToLower()] = NameToIdx.Count;
        }

        ~PropertyManager()
        {
            Cache.Remove(Type);
        }

        public static PropertyManager Open<T>() => Open(typeof(T));

        public static PropertyManager Open(Type _type)
        {
            if (Cache.TryGetValue(_type, out var _cached) == false)
                Cache[_type] = _cached = new(_type);

            return _cached;
        }

        public int GetIndex(string _name) => NameToIdx[_name.Trim().ToLower()];
        public string GetName(int _idx) => Properties[_idx].Name;

        public PropertyInfo GetInfo(int _idx) => Properties[_idx];

        public PropertyInfo GetInfo(string _name) => GetInfo(GetIndex(_name));

        public Type GetType(int _idx) => GetInfo(_idx).PropertyType;

        public Type GetType(string _name) => GetInfo(_name).PropertyType;

        public object GetValue(object _ref, int _idx) => GetInfo(_idx).GetValue(_ref);

        public object GetValue(object _ref, string _name) => GetInfo(_name).GetValue(_ref);

        public void SetValue(object _ref, int _idx, object _value) => GetInfo(_idx).SetValue(_ref, _value);

        public void SetValue(object _ref, string _name, object _value) => GetInfo(_name).SetValue(_ref, _value);
    }
}