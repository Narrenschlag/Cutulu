namespace Cutulu
{
    using System.Collections.Generic;

    public static class CollectionExtension
    {
        public static bool NotEmpty<T>(this ICollection<T> collection) => collection != null && collection.Count > 0;
        public static bool IsEmpty<T>(this ICollection<T> collection) => !NotEmpty(collection);

        public static int Size<T>(this ICollection<T> collection) => collection.NotEmpty() ? collection.Count : 0;
    }
}