namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Collections;
    using System;

    public static class Collectionf
    {
        public static bool NotEmpty<T>(this ICollection<T> collection) => collection != null && collection.Count > 0;
        public static bool IsEmpty<T>(this ICollection<T> collection) => !NotEmpty(collection);

        public static int Size<T>(this ICollection<T> collection) => collection != null ? collection.Count : 0;

        public static T[] ToArray<T>(this ICollection<T> collection)
        {
            if (collection.NotEmpty())
            {
                var array = new T[collection.Count];

                collection.CopyTo(array, 0);
                return array;
            }

            return Array.Empty<T>();
        }

        public static T[] ToArray<T>(this ICollection collection)
        {
            if (collection != null && collection.Count > 0)
            {
                var array = new T[collection.Count];

                collection.CopyTo(array, 0);
                return array;
            }

            return Array.Empty<T>();
        }
    }
}