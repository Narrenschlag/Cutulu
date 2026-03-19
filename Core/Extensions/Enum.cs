namespace Cutulu.Core
{
    using System;

    public static class Enumf
    {
        public static Array Array(this Enum Enum) => Enum.GetValues(Enum.GetType());
        public static int Length(this Enum Enum) => Array(Enum).Length;

        public static Array EnumArray<T>() where T : Enum => Enum.GetValues(typeof(T));
        public static int EnumLength<T>() where T : Enum => EnumArray<T>().Length;

        public static Type GetEnumType(this Enum Enum) => Enum.GetType().GetEnumUnderlyingType();

        public static bool HasAnyOverlap<E>(this E a, E b) where E : Enum
        {
            return (Convert.ToInt64(a) & Convert.ToInt64(b)) != 0;
        }
    }
}