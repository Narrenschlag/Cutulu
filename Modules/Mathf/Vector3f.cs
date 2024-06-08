using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public static class Vector3f
    {
        // Cross product for Vector3
        public static Vector3 Cross(this Vector3 vectorA, Vector3 vectorB)
        {
            return new Vector3(
                vectorA.Y * vectorB.Z - vectorA.Z * vectorB.Y,
                vectorA.Z * vectorB.X - vectorA.X * vectorB.Z,
                vectorA.X * vectorB.Y - vectorA.Y * vectorB.X
            );
        }

        public static (Vector3 min, Vector3 max) MinMax(params Vector3[] values) => (Min(values), Min(values));

        public static Vector3 Min(params Vector3[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                for (byte k = 0; k < 3; k++)
                {
                    value[k] = Mathf.Min(value[k], values[i][k]);
                }
            }

            return value;
        }

        public static Vector3 Max(params Vector3[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                for (byte k = 0; k < 3; k++)
                {
                    value[k] = Mathf.Max(value[k], values[i][k]);
                }
            }

            return value;
        }

        // Function to compute the centroid of a list of Vector3 points
        public static Vector3 ComputeCentroid(ICollection<Vector3> points)
        {
            var centroid = new Vector3();

            foreach (var point in points)
            {
                centroid += point;
            }

            return centroid / points.Count;
        }

        // Function to compute the angle between a point and the centroid
        public static float ComputeAngle(Vector3 centroid, Vector3 point)
        => Mathf.Atan2(point.Z - centroid.Z, point.X - centroid.X);

        // Function to order the points clockwise
        public static List<Vector3> OrderClockwise2(this ICollection<Vector3> points)
        {
            var list = new List<Vector3>(points);
            if (list.Count < 3) return list; // No need to sort if less than 3 points

            var centroid = ComputeCentroid(list);
            list.Sort((a, b) =>
            {
                var angleA = ComputeAngle(centroid, a);
                var angleB = ComputeAngle(centroid, b);
                return angleB.CompareTo(angleA); // For clockwise sorting
            });

            return list;
        }

        public static List<Vector3> OrderClockwise(this ICollection<Vector3> _points, bool flip = true)
        {
            var points = new List<Vector3>(_points);
            var crossProduct = 0f;

            // Calculate the cross product of adjacent edges
            for (int i = 0; i < points.Count; i++)
            {
                var current = points[i];
                var next = points[(i + 1) % points.Count]; // Wrap around to the first point when reaching the last one
                var prev = points[(i + points.Count - 1) % points.Count]; // Wrap around to the last point when reaching the first one

                var currentToNext = next - current;
                var prevToCurrent = current - prev;

                crossProduct += currentToNext.X * prevToCurrent.Z - currentToNext.Z * prevToCurrent.X;
            }

            // If the cross product is negative, points are ordered clockwise
            if (flip ? crossProduct >= 0 : crossProduct < 0) points.Reverse();
            return points;
        }

        public static bool TryIntersectFlat(this Vector3 A, Vector3 a, Vector3 B, Vector3 b, out Vector3 C)
        {
            var result = A.toXY().TryIntersect(a.toXY(), B.toXY(), b.toXY(), out var c);

            C = c.toXZ();
            return result;
        }
    }
}