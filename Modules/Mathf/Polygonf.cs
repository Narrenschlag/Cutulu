using System.Collections.Generic;
using System;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// The Polygonf class provides static methods for triangulating a polygon represented by a collection of Vector3 vertices in a 2D space.
    /// This class is part of the Cutulu SDK by Maximilian Schecklmann and is designed to work with the Godot API.
    /// </summary>
    public static class Polygonf
    {
        #region Vector3
        /// <summary>
        /// Triangulates a polygon represented by an array of Vector3 vertices.
        /// </summary>
        /// <param name="vertices">The array of Vector3 vertices defining the polygon in 2D space.</param>
        /// <returns>An array of integers representing the indices of the vertices that form triangles.</returns>
        public static List<int> Triangulate(this Vector3[] vertices)
        {
            var indices = new List<int>();
            var n = vertices.Length;

            if (n < 3)
                return indices;

            // Create a list of vertex indices
            var V = new List<int>();
            for (var v = 0; v < n; v++)
                V.Add(v);

            var count = 2 * n;   // Safety counter to avoid infinite loops
            var i = 0;

            while (n > 2)
            {
                if (count-- <= 0)
                {
                    //Debug.LogError("Triangulation failed: infinite loop detected");
                    return indices; // Triangulation failed
                }

                // Three consecutive vertices in the polygon
                var u = (i + n - 1) % n; // Previous vertex
                var v = i % n;           // Current vertex
                var w = (i + 1) % n;     // Next vertex

                //Debug.Log($"Checking vertices {V[u]}, {V[v]}, {V[w]}");

                if (IsEar(u, v, w, V, vertices))
                {
                    //Debug.Log($"Found ear: {V[u]}, {V[v]}, {V[w]}");

                    // Add the triangle u-v-w to the result
                    indices.Add(V[u]);
                    indices.Add(V[v]);
                    indices.Add(V[w]);

                    // Remove v from the polygon
                    V.RemoveAt(v);

                    // Update the number of remaining vertices
                    n--;

                    // Reset the counter
                    count = 2 * n;

                    // Adjust the current index to the previous one
                    i = Math.Max(i - 1, 0);
                }
                else
                {
                    //Debug.Log($"No ear found at vertices {V[u]}, {V[v]}, {V[w]}");
                    // Move to the next vertex
                    i++;
                }
            }

            return indices;
        }

        private static bool IsEar(int u, int v, int w, List<int> V, Vector3[] vertices)
        {
            var A = vertices[V[u]];
            var B = vertices[V[v]];
            var C = vertices[V[w]];

            //Debug.Log($"Checking ear: {V[u]} ({A}), {V[v]} ({B}), {V[w]} ({C})");

            if (Area(A, B, C) <= 0)
            {
                //Debug.Log($"Not an ear (area check failed): {V[u]}, {V[v]}, {V[w]}");
                return false;
            }

            for (var p = 0; p < V.Count; p++)
            {
                if (p == u || p == v || p == w)
                    continue;

                var P = vertices[V[p]];
                if (PointInTriangle(P, A, B, C))
                {
                    //Debug.Log($"Not an ear (point in triangle): {V[u]}, {V[v]}, {V[w]} with point {V[p]} ({P})");
                    return false;
                }
            }

            //Debug.Log($"Ear detected: {V[u]}, {V[v]}, {V[w]}");
            return true;
        }

        private static float Area(Vector3 A, Vector3 B, Vector3 C)
        {
            return (B.X - A.X) * (C.Z - A.Z) - (C.X - A.X) * (B.Z - A.Z);
        }

        private static bool PointInTriangle(Vector3 P, Vector3 A, Vector3 B, Vector3 C)
        {
            static bool IsPointInTriangle(Vector3 pt, Vector3 v1, Vector3 v2, Vector3 v3)
            {
                var dX = pt.X - v3.X;
                var dY = pt.Z - v3.Z;
                var dX21 = v3.X - v2.X;
                var dY12 = v2.Z - v3.Z;
                var D = dY12 * (v1.X - v3.X) + dX21 * (v1.Z - v3.Z);
                var s = dY12 * dX + dX21 * dY;
                var t = (v3.Z - v1.Z) * dX + (v1.X - v3.X) * dY;
                if (D < 0) return s <= 0 && t <= 0 && s + t >= D;
                return s >= 0 && t >= 0 && s + t <= D;
            }

            return IsPointInTriangle(P, A, B, C);
        }
        #endregion

        #region Vector2
        /// <summary>
        /// Triangulates a polygon represented by an array of Vector2 vertices.
        /// </summary>
        /// <param name="vertices">The array of Vector2 vertices defining the polygon in 2D space.</param>
        /// <returns>An array of integers representing the indices of the vertices that form triangles.</returns>
        public static int[] Triangulate(this Vector2[] vertices)
        {
            var indices = new List<int>();
            var n = vertices.Length;

            if (n < 3)
                return indices.ToArray();

            // Create a list of vertex indices
            var V = new List<int>();
            for (var v = 0; v < n; v++)
                V.Add(v);

            var count = 2 * n;   // Safety counter to avoid infinite loops
            var i = 0;

            while (n > 2)
            {
                if (count-- <= 0)
                {
                    //Debug.LogError("Triangulation failed: infinite loop detected");
                    return indices.ToArray(); // Triangulation failed
                }

                // Three consecutive vertices in the polygon
                var u = (i + n - 1) % n; // Previous vertex
                var v = i % n;           // Current vertex
                var w = (i + 1) % n;     // Next vertex

                //Debug.Log($"Checking vertices {V[u]}, {V[v]}, {V[w]}");

                if (IsEar(u, v, w, V, vertices))
                {
                    //Debug.Log($"Found ear: {V[u]}, {V[v]}, {V[w]}");

                    // Add the triangle u-v-w to the result
                    indices.Add(V[u]);
                    indices.Add(V[v]);
                    indices.Add(V[w]);

                    // Remove v from the polygon
                    V.RemoveAt(v);

                    // Update the number of remaining vertices
                    n--;

                    // Reset the counter
                    count = 2 * n;

                    // Adjust the current index to the previous one
                    i = Math.Max(i - 1, 0);
                }
                else
                {
                    //Debug.Log($"No ear found at vertices {V[u]}, {V[v]}, {V[w]}");
                    // Move to the next vertex
                    i++;
                }
            }

            return indices.ToArray();
        }

        private static bool IsEar(int u, int v, int w, List<int> V, Vector2[] vertices)
        {
            var A = vertices[V[u]];
            var B = vertices[V[v]];
            var C = vertices[V[w]];

            //Debug.Log($"Checking ear: {V[u]} ({A}), {V[v]} ({B}), {V[w]} ({C})");

            if (Area(A, B, C) <= 0)
            {
                //Debug.Log($"Not an ear (area check failed): {V[u]}, {V[v]}, {V[w]}");
                return false;
            }

            for (var p = 0; p < V.Count; p++)
            {
                if (p == u || p == v || p == w)
                    continue;

                var P = vertices[V[p]];
                if (PointInTriangle(P, A, B, C))
                {
                    //Debug.Log($"Not an ear (point in triangle): {V[u]}, {V[v]}, {V[w]} with point {V[p]} ({P})");
                    return false;
                }
            }

            //Debug.Log($"Ear detected: {V[u]}, {V[v]}, {V[w]}");
            return true;
        }

        private static float Area(Vector2 A, Vector2 B, Vector2 C)
        {
            return (B.X - A.X) * (C.Y - A.Y) - (C.X - A.X) * (B.Y - A.Y);
        }

        private static bool PointInTriangle(Vector2 P, Vector2 A, Vector2 B, Vector2 C)
        {
            static bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
            {
                var dX = pt.X - v3.X;
                var dY = pt.Y - v3.Y;
                var dX21 = v3.X - v2.X;
                var dY12 = v2.Y - v3.Y;
                var D = dY12 * (v1.X - v3.X) + dX21 * (v1.Y - v3.Y);
                var s = dY12 * dX + dX21 * dY;
                var t = (v3.Y - v1.Y) * dX + (v1.X - v3.X) * dY;
                if (D < 0) return s <= 0 && t <= 0 && s + t >= D;
                return s >= 0 && t >= 0 && s + t <= D;
            }

            return IsPointInTriangle(P, A, B, C);
        }
        #endregion
    }
}