using System.Collections.Generic;
using System.Reflection;

namespace Cutulu
{
    public static class DataStructure
    {
        public static List<FieldInfo> ReadValueInfos<T>()
        {
            BindingFlags bindingFlags = BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance |
            BindingFlags.Static;

            List<FieldInfo> valueNames = new List<FieldInfo>();
            foreach (FieldInfo field in typeof(T).GetFields(bindingFlags))
            {
                valueNames.Add(field);
            }

            return valueNames;
        }

        public static void SetFieldValue<T, V>(this T t, string valueName, V value)
            => typeof(T).GetProperty(valueName).SetValue(t, value, null);

        public static V GetFieldValue<T, V>(this T t, string valueName)
        {
            object value = typeof(T).GetProperty(valueName).GetValue(t, null);

            return value.Equals(default(V)) ? default(V) : (V)value;
        }
    }
}