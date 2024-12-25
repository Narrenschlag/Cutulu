namespace Cutulu
{
    using System.Collections.Generic;
    using System.Collections;

    public static class CollectionExtension
    {
        public static bool NotEmpty<T>(this ICollection<T> collection) => collection != null && collection.Count > 0;
        public static bool IsEmpty<T>(this ICollection<T> collection) => !NotEmpty(collection);

        public static int Size<T>(this ICollection<T> collection) => collection.NotEmpty() ? collection.Count : 0;

        public static T[] ToArray<T>(this ICollection<T> collection) => ToList(collection).ToArray();
        public static List<T> ToList<T>(this ICollection<T> collection) => new(collection);

        public static T[] ToArray<T>(this ICollection collection)
        {
            var list = new List<T>();

            foreach (var item in collection)
            {
                if (item is T t) list.Add(t);
            }

            return list.ToArray();
        }
    }
}