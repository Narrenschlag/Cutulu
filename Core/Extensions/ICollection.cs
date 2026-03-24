namespace Cutulu.Core
{
    using System.Runtime.CompilerServices;
    using System.Collections.Generic;
    using System.Collections;

    public static class Collectionf
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotEmpty<T>(this IEnumerable<T> collection)
        {
            if (collection != null)
            {
                switch (collection)
                {
                    case ICollection<T> c: return c.Count > 0;
                    case IReadOnlyCollection<T> r: return r.Count > 0;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty<T>(this IEnumerable<T> collection) => !NotEmpty(collection);

        public static bool NotEmpty<T>(this T[] collection) => collection != null && collection.Length > 0;
        public static bool IsEmpty<T>(this T[] collection) => !NotEmpty(collection);

        public static int Size<T>(this ICollection<T> collection) => collection != null ? collection.Count : 0;

        public static T[] ToArray<T>(this ICollection<T> collection)
        {
            if (collection.NotEmpty())
            {
                var array = new T[collection.Count];

                collection.CopyTo(array, 0);
                return array;
            }

            return [];
        }

        public static T[] ToArray<T>(this ICollection collection)
        {
            if (collection != null && collection.Count > 0)
            {
                var array = new T[collection.Count];

                collection.CopyTo(array, 0);
                return array;
            }

            return [];
        }
    }
}