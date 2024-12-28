namespace Cutulu.Core
{
    using System;
    using Godot;

    public static class Hexagon
    {
        public const int Num = 6;

        /// <summary>
        /// Returns the total cell count of a range including all rings from 0 to the specified ringCount.
        /// </summary>
        public static int GetCellCountInRange(int ringCount)
        {
            return ringCount < 1 ? 1 : 1 + 3 * ringCount * (ringCount + 1);
        }

        /// <summary>
        /// Returns the cell count of a single ring.
        /// </summary>
        public static int GetCellCountInRing(int ringCount)
        {
            return ringCount < 1 ? 1 : Num * ringCount;
        }

        /// <summary>
        /// Converts world position to key
        /// </summary>
        public static T ToKey<T>(this Vector3 position, Orientation orientation)
        {
            return default(T) switch
            {
                int _ => (T)(object)Hexagon1.ToIndex(position, orientation),

                Vector2I _ => (T)(object)Hexagon2.ToAxial(position, orientation),

                Vector3I _ => (T)(object)Hexagon3.ToCubic(position, orientation),

                _ => default,
            };
        }

        /// <summary>
        /// Converts key to world position
        /// </summary>
        public static Vector3 ToPosition<T>(this T key, Orientation orientation)
        {
            return key switch
            {
                int k => Hexagon1.ToWorld(k, orientation),

                Vector2I k => Hexagon2.ToWorld(k, orientation),

                Vector3I k => Hexagon3.ToWorld(k, orientation),

                _ => default,
            };
        }

        /// <summary>
        /// Returns neighbour of given key
        /// </summary>
        public static T GetNeighbour<T>(this T key, int neighbourIndex)
        {
            return key switch
            {
                int k => (T)(object)Hexagon1.GetNeighbour(k, neighbourIndex),

                Vector2I k => (T)(object)Hexagon2.GetNeighbour(k, neighbourIndex),

                Vector3I k => (T)(object)Hexagon3.GetNeighbour(k, neighbourIndex),

                _ => default,
            };
        }

        /// <summary>
        /// Returns vertices of given key
        /// </summary>
        public static Vector3[] GetVertices<T>(Orientation orientation)
        {
            return default(T) switch
            {
                int _ => Hexagon1.GetVertices(orientation),

                Vector2I _ => Hexagon2.GetVertices(orientation),

                Vector3I _ => Hexagon3.GetVertices(orientation),

                _ => Array.Empty<Vector3>(),
            };
        }

        /// <summary>
        /// Returns vertices of given key
        /// </summary>
        public static Vector3[] GetVertices<T>(this T key, Orientation orientation)
        {
            return key switch
            {
                int k => Hexagon1.GetVertices(k, orientation),

                Vector2I k => Hexagon2.GetVertices(k, orientation),

                Vector3I k => Hexagon3.GetVertices(k, orientation),

                _ => Array.Empty<Vector3>(),
            };
        }

        /// <summary>
        /// Returns vertice of given key
        /// </summary>
        public static Vector3 GetVertice<T>(this T key, int index, Orientation orientation)
        {
            return key switch
            {
                int k => Hexagon1.GetVertice(k, index, orientation),

                Vector2I k => Hexagon2.GetVertice(k, index, orientation),

                Vector3I k => Hexagon3.GetVertice(k, index, orientation),

                _ => default,
            };
        }

        /// <summary>
        /// Returns range around center
        /// </summary>
        public static T[] GetRange<T>(int ringCount) => GetRange(default(T), ringCount);

        /// <summary>
        /// Returns range around given key
        /// </summary>
        public static T[] GetRange<T>(this T key, int ringCount)
        {
            return key switch
            {
                int k => (T[])(object)Hexagon1.GetRange(k, ringCount),

                Vector2I k => (T[])(object)Hexagon2.GetRange(k, ringCount),

                Vector3I k => (T[])(object)Hexagon3.GetRange(k, ringCount),

                _ => Array.Empty<T>(),
            };
        }

        /// <summary>
        /// Returns ring around center
        /// </summary>
        public static T[] GetRing<T>(int ring) => GetRing(default(T), ring);

        /// <summary>
        /// Returns ring around given key
        /// </summary>
        public static T[] GetRing<T>(this T key, int ring)
        {
            return key switch
            {
                int k => (T[])(object)Hexagon1.GetRing(k, ring),

                Vector2I k => (T[])(object)Hexagon2.GetRing(k, ring),

                Vector3I k => (T[])(object)Hexagon3.GetRing(k, ring),

                _ => Array.Empty<T>(),
            };
        }
    }
}