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
        /// Try calculate the intersection point C of two points and their directions
        /// </summary>
        public static bool TryIntersect(this Vector2 A, Vector2 a, Vector2 B, Vector2 b, out Vector2 C)
        {
            // Normalize values
            a = a.Normalized();
            b = b.Normalized();

            // Check if directions are parallel
            if (Mathf.Abs(a.Cross(b)) < Mathf.Epsilon)
            {
                C = default;
                return false;
            }

            // Calculate t and s
            var t = (B.X * b.Y - B.Y * b.X - A.X * b.Y + A.Y * b.X) / (a.X * b.Y - a.Y * b.X);
            var s = (A.X + t * a.X - B.X) / b.X;

            // Check for "Kollinearität (Berührung oder Übereinanderliegen)"
            // -> meaning that the lines are on each other and have infinite intersections
            if (Mathf.Abs(t) < Mathf.Epsilon && Mathf.Abs(s) < Mathf.Epsilon)
            {
                C = default;
                return false;
            }

            // Check for non intersecting vectors
            if (s < 0 || s > 1)
            {
                C = default;
                return false;
            }

            // Calculate result
            C = A + t * a;
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
    }
}