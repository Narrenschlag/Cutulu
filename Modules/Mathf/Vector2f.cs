using System.Collections.Generic;
using System;
using Godot;

namespace Cutulu
{
    public static class Vector2f
    {
        // Cross product for Vector2 (returns a zero Vector3)
        public static float Cross(this Vector2 vectorA, Vector2 vectorB)
        {
            return vectorA.X * vectorB.Y - vectorA.Y * vectorB.X;
        }

        /// <summary>
        /// Determines if two lines intersect and returns the intersection point.
        /// </summary>
        /// <param name="A">Origin of the first line.</param>
        /// <param name="a">Direction of the first line.</param>
        /// <param name="B">Origin of the second line.</param>
        /// <param name="b">Direction of the second line.</param>
        /// <param name="intersection">Output intersection point if the lines intersect.</param>
        /// <returns>True if the lines intersect, otherwise false.</returns>
        public static bool TryIntersect(this Vector2 A, Vector2 a, Vector2 B, Vector2 b, out Vector2 intersection)
        {
            float denominator = a.X * b.Y - a.Y * b.X;
            intersection = default;

            // Check if lines are parallel (denominator is zero)
            if (Math.Abs(denominator) < Mathf.Epsilon)
            {
                return false;
            }

            float t = ((B.X - A.X) * b.Y - (B.Y - A.Y) * b.X) / denominator;

            intersection = A + t * a;
            return true;
        }

        public static (Vector2 min, Vector2 max) MinMax(params Vector2[] values) => (Min(values), Min(values));

        public static Vector2 Min(params Vector2[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                for (byte k = 0; k < 2; k++)
                {
                    value[k] = Mathf.Min(value[k], values[i][k]);
                }
            }

            return value;
        }

        public static Vector2 Max(params Vector2[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                for (byte k = 0; k < 2; k++)
                {
                    value[k] = Mathf.Max(value[k], values[i][k]);
                }
            }

            return value;
        }

        public static Vector2 Average(params Vector2[] corners)
        {
            var sum = corners[0];

            for (int i = 1; i < corners.Length; i++)
            {
                sum += corners[i];
            }

            return sum / corners.Length;
        }

        public static Vector2 ClampNormalized(this Vector2 value)
        {
            var normalized = value.Normalized();
            return new(
                Mathf.Abs(value.X) > Mathf.Abs(normalized.X) ? normalized.X : value.X,
                Mathf.Abs(value.Y) > Mathf.Abs(normalized.Y) ? normalized.Y : value.Y
            );
        }

        public static Vector2[] InterpolateEvenly(this Vector2[] originalPoints, int targetLength)
        {
            if (originalPoints == null || originalPoints.Length < 2 || targetLength < 2)
            {
                throw new ArgumentException("Invalid input data.");
            }

            // Step 1: Calculate cumulative distances
            var cumulativeDistances = new float[originalPoints.Length];
            cumulativeDistances[0] = 0f;

            for (int i = 1; i < originalPoints.Length; i++)
            {
                cumulativeDistances[i] = cumulativeDistances[i - 1] + originalPoints[i - 1].DistanceTo(originalPoints[i]);
            }

            var totalDistance = cumulativeDistances[^1];
            var intervalDistance = totalDistance / (targetLength - 1);

            // Step 2: Interpolate new points at evenly spaced intervals
            var newPoints = new List<Vector2>()
            {
                originalPoints[0]
            };

            for (int i = 1; i < targetLength - 1; i++)
            {
                var targetDistance = i * intervalDistance;
                var newPoint = InterpolateAtDistance(originalPoints, cumulativeDistances, targetDistance);
                newPoints.Add(newPoint);
            }

            newPoints.Add(originalPoints[^1]);

            return newPoints.ToArray();

            static Vector2 InterpolateAtDistance(Vector2[] points, float[] cumulativeDistances, float targetDistance)
            {
                for (int i = 1; i < points.Length; i++)
                {
                    if (cumulativeDistances[i] >= targetDistance)
                    {
                        var segmentStartDist = cumulativeDistances[i - 1];
                        var segmentEndDist = cumulativeDistances[i];
                        var segmentLength = segmentEndDist - segmentStartDist;

                        var t = (targetDistance - segmentStartDist) / segmentLength;
                        return points[i - 1].Lerp(points[i], t);
                    }
                }

                return points[^1]; // Should never reach here if inputs are valid
            }
        }

        public static Vector2I Min(this Vector2I a, Vector2I b) => new(Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y));
        public static Vector2I Max(this Vector2I a, Vector2I b) => new(Mathf.Max(a.X, b.X), Mathf.Max(a.Y, b.Y));

        public static Vector2 Random(this Vector2 a, Vector2 b)
        {
            return new(Cutulu.Random.Range(a.X, b.X), Cutulu.Random.Range(a.Y, b.Y));
        }

        public static Vector2 Sum(this Vector2[] b)
        {
            if (b.IsEmpty()) return default;
            var sum = Vector2.Zero;

            for (ushort i = 0; i < b.Length; i++)
            {
                sum += b[i];
            }

            return sum;
        }

        public static Vector2I Sum(this Vector2I[] b)
        {
            if (b.IsEmpty()) return default;
            var sum = Vector2I.Zero;

            for (ushort i = 0; i < b.Length; i++)
            {
                sum += b[i];
            }

            return sum;
        }
    }
}