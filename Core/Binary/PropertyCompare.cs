namespace Cutulu.Core
{
    using System.Collections;

    /// <summary>
    /// Provides deep comparison logic for any object, including Godot.Resources, collections, and dictionaries.
    /// </summary>
    public static class PropertyCompare
    {
        /// <summary>
        /// Performs a deep comparison of two objects, including collections and Godot.Resource instances.
        /// </summary>
        /// <param name="a">The first object to compare.</param>
        /// <param name="b">The second object to compare.</param>
        /// <returns>True if the objects are deeply equal; otherwise, false.</returns>
        public static bool IsEqualTo(this object a, object b, bool deepBinaryComparison = false)
        {
            if (a.IsNull() != b.IsNull()) return false;
            if (a.IsNull() || ReferenceEquals(a, b)) return true;
            if (a.GetType() != b.GetType()) return false;

            // Handle deep binary comparisons by encoding both and comparing their buffer
            if (deepBinaryComparison) return a.Encode().SequenceEquals(b.Encode());

            // Handle dictionaries
            if (a is IDictionary dictA && b is IDictionary dictB)
                return AreDictionariesEqual(dictA, dictB);

            // Handle collections (e.g., List, Array)
            if (a is ICollection colA && b is ICollection colB)
                return AreCollectionsEqual(colA, colB);

            // Handle general enumerables
            if (a is IEnumerable enumA && b is IEnumerable enumB)
                return AreEnumerablesEqual(enumA, enumB);

            // Fallback to default equality
            return Equals(a, b);
        }

        private static bool AreDictionariesEqual(IDictionary a, IDictionary b)
        {
            if (a.Count != b.Count) return false;

            foreach (var key in a.Keys)
            {
                if (!b.Contains(key)) return false;
                if (!a[key].IsEqualTo(b[key])) return false;
            }

            return true;
        }

        private static bool AreCollectionsEqual(ICollection a, ICollection b)
        {
            if (a.Count != b.Count) return false;

            var enumA = a.GetEnumerator();
            var enumB = b.GetEnumerator();

            while (enumA.MoveNext() && enumB.MoveNext())
            {
                if (!enumA.Current.IsEqualTo(enumB.Current)) return false;
            }

            return true;
        }

        private static bool AreEnumerablesEqual(IEnumerable a, IEnumerable b)
        {
            var enumA = a.GetEnumerator();
            var enumB = b.GetEnumerator();

            while (true)
            {
                var hasA = enumA.MoveNext();
                var hasB = enumB.MoveNext();

                if (hasA != hasB) return false;
                if (!hasA) break;

                if (!enumA.Current.IsEqualTo(enumB.Current)) return false;
            }

            return true;
        }
    }
}
