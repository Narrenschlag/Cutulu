namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using System;

    public static partial class PropertyReflection
    {
        // Saved to improved performance for repetetive usage of same types
        private readonly static Dictionary<Type, PropertyInfo[]> Cache = [];

        public static void ClearCache()
        {
            Cache.Clear();
        }

        public static PropertyInfo[] GetSetProperties(this Type _type)
        {
            // Get properties that have both getter and setter
            if (Cache.TryGetValue(_type, out var _cached) == false)
                Cache[_type] = _cached = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(p => p.CanRead && p.CanWrite)
                                        .ToArray();

            return _cached;
        }

        public static PropertyInfo GetSetPropertyByIdx(this Type _type, int _idx)
        {
            return GetSetProperties(_type)[_idx];
        }

        public static Type GetPropertyType(this Type _type, int _idx)
        {
            return GetSetPropertyByIdx(_type, _idx).PropertyType;
        }

        public static object GetTypeValue(this object _obj, PropertyInfo _property)
        {
            return _property.GetValue(_obj);
        }

        public static object GetTypeValue(this object _obj, int _idx)
        {
            return GetTypeValue(_obj, GetSetPropertyByIdx(_obj.GetType(), _idx));
        }

        public static void ApplyProperty(this object _obj, PropertyInfo _property, object _value)
        {
            _property.SetValue(_obj, _value);
        }

        public static void ApplyProperty(this object _obj, int _idx, object _value)
        {
            ApplyProperty(_obj, GetSetPropertyByIdx(_obj.GetType(), _idx), _value);
        }
    }
}