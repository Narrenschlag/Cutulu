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
        public static void ClearCache() => Cache.Clear();

        public static PropertyInfo[] GetSetProperties(this Type _type)
        {
            // Get properties that have both getter and setter
            if (Cache.TryGetValue(_type, out var _cached) == false)
                Cache[_type] = _cached = [..
                    _type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead && p.CanWrite)
                ];

            return _cached;
        }
    }
}