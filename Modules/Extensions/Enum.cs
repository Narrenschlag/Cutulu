namespace Cutulu
{
    using System;

    public static class EnumExtension
    {
        public static Array Array(this Enum Enum) => Enum.GetValues(Enum.GetType());
        public static int Length(this Enum Enum) => Array(Enum).Length;

        public static Array EnumArray<T>() where T : Enum => Enum.GetValues(typeof(T));
        public static int EnumLength<T>() where T : Enum => EnumArray<T>().Length;

        public static Type GetEnumType(this Enum Enum) => Enum.GetType().GetEnumUnderlyingType();
    }
}