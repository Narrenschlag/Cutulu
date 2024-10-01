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

        public static List<Vector3> OrderClockwise(this ICollection<Vector3> _points, bool flip = true) => OrderClockwise(_points, out _, flip);
        public static List<Vector3> OrderClockwise(this ICollection<Vector3> _points, out bool flipped, bool flip = true)
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
            if (flip ? crossProduct >= 0 : crossProduct < 0)
            {
                points.Reverse();
                flipped = true;
            }

            else flipped = false;

            return points;
        }

        public static bool TryIntersectFlat(this Vector3 A, Vector3 a, Vector3 B, Vector3 b, out Vector3 C)
        {
            var result = A.toXY().TryIntersect(a.toXY(), B.toXY(), b.toXY(), out var c);

            C = c.toXZ();
            return result;
        }

        public static Vector3 IntersectAt(this Vector3 origin, Vector3 direction, float y)
        {
            var origin0 = origin.setY(y);
            var dir0 = direction.setY(y);

            if (origin.TryIntersect(direction, origin0, dir0, out var intersection))
            {
                return intersection;
            }

            return default;
        }

        public static bool TryIntersect(this Vector3 origin1, Vector3 direction1, Vector3 origin2, Vector3 direction2, out Vector3 intersection)
        {
            // Ensure direction vectors are normalized
            var d1 = direction1.Normalized();
            var d2 = direction2.Normalized();

            // Calculate the cross product of the direction vectors
            var crossD1D2 = d1.Cross(d2);

            // If cross product is zero, the lines are parallel or collinear
            if (crossD1D2.Length() == 0)
            {
                intersection = default;
                return false; // Lines are parallel, no intersection
            }

            // Calculate the vector between the origins
            var originDiff = origin2 - origin1;

            // Calculate the determinants
            var denominator = crossD1D2.LengthSquared();
            var t1 = originDiff.Cross(d2).Dot(crossD1D2) / denominator;
            var t2 = originDiff.Cross(d1).Dot(crossD1D2) / denominator;

            // Calculate the potential intersection points
            var pointOnLine1 = origin1 + t1 * d1;
            var pointOnLine2 = origin2 + t2 * d2;

            // Check if intersection points are the same (within a small tolerance)
            if (pointOnLine1.DistanceTo(pointOnLine2) < 0.001f)
            {
                intersection = pointOnLine1; // Intersection point
                return true;
            }
            else
            {
                intersection = default;
                return false; // No intersection found
            }
        }

        public static Vector3 Sum(this Vector3[] b)
        {
            if (b.IsEmpty()) return default;
            var sum = Vector3.Zero;

            for (ushort i = 0; i < b.Length; i++)
            {
                sum += b[i];
            }

            return sum;
        }

        public static Vector3I Sum(this Vector3I[] b)
        {
            if (b.IsEmpty()) return default;
            var sum = Vector3I.Zero;

            for (ushort i = 0; i < b.Length; i++)
            {
                sum += b[i];
            }

            return sum;
        }
    }
}