namespace Cutulu.Core
{
    using System.Collections.Generic;
    using Godot;

    public static class Vector3Extension
    {
        public static void SetForward(this Node3D node, Vector3 direction, bool global = true) => node.LookAt((global ? node.GlobalPosition : node.Position) + direction);

        public static Vector3I RoundToInt(this Vector3 v3) => new(Mathf.RoundToInt(v3.X), Mathf.RoundToInt(v3.Y), Mathf.RoundToInt(v3.Z));
        public static Vector3I FloorToInt(this Vector3 v3) => new(Mathf.FloorToInt(v3.X), Mathf.FloorToInt(v3.Y), Mathf.FloorToInt(v3.Z));
        public static Vector3I CeilToInt(this Vector3 v3) => new(Mathf.CeilToInt(v3.X), Mathf.CeilToInt(v3.Y), Mathf.CeilToInt(v3.Z));

        public static Vector3 setX(this Vector3 v3, float value) => new(value, v3.Y, v3.Z);
        public static Vector3 setY(this Vector3 v3, float value) => new(v3.X, value, v3.Z);
        public static Vector3 setZ(this Vector3 v3, float value) => new(v3.X, v3.Y, value);
        public static Vector3 multX(this Vector3 v3, float value) => new(v3.X * value, v3.Y, v3.Z);
        public static Vector3 multY(this Vector3 v3, float value) => new(v3.X, v3.Y * value, v3.Z);
        public static Vector3 multZ(this Vector3 v3, float value) => new(v3.X, v3.Y, v3.Z * value);

        public static void pasteX(this float value, ref Vector3 v3) => v3.X = value;
        public static void pasteY(this float value, ref Vector3 v3) => v3.Y = value;
        public static void pasteZ(this float value, ref Vector3 v3) => v3.Z = value;

        public static Vector3 toRight(this Vector3 forward) => toRight(forward, Vector3.Up);
        public static Vector3 toRight(this Vector3 forward, Vector3 up) => forward.Cross(up).Normalized();

        public static Vector3 toUp(this Vector3 forward) => toRight(forward, Vector3.Right);
        public static Vector3 toUp(this Vector3 forward, Vector3 right) => -forward.Cross(right).Normalized();

        public static Vector3 toXZ(this Vector2 value, float y = 0) => new(value.X, y, value.Y);

        /// <summary>
        /// Round Vector3 to given decimal spaces
        /// </summary>
        public static Vector3 Round(this Vector3 value, byte decimalSpaces = 0)
        => new(value.X.Round(decimalSpaces), value.Y.Round(decimalSpaces), value.Z.Round(decimalSpaces));

        /// <summary>
        /// Round Vector3 to given decimal spaces
        /// </summary>
        public static Vector3 Round(this Vector3 value, float step = 1f)
        => new(value.X.Round(step), value.Y.Round(step), value.Z.Round(step));

        public static float Round(this float value, byte decimalSpaces)
        => Mathf.RoundToInt(value * Mathf.Pow(10, decimalSpaces)) / Mathf.Pow(10, decimalSpaces);

        public static float Round(this float value, float step = 1f)
        {
            if (step <= 0) throw new System.ArgumentException("Step must be greater than zero.");

            float remainder = (value = Mathf.Ceil(value / 0.001f) * 0.001f) % step;
            float halfStep = step / 2f;

            return
                remainder >= halfStep ? value + step - remainder :
                remainder < -halfStep ? value - step - remainder :
                value - remainder;
        }

        public static float GetYRotation(this Vector3 direction, bool useRadians = false)
        {
            // Ensure the direction is normalized
            direction = direction.Normalized();

            // Calculate the angle using the arctangent function
            // Adjust the angle to be positive and between 0 and 360 units
            float angle = (Mathf.Atan2(direction.X, direction.Z) + Mathf.Pi * 2) % (Mathf.Pi * 2);

            // Convert the angle to degrees if needed
            return useRadians ? angle : angle.toDegrees();
        }

        public static Vector3 GetDirectionFromYRotation(this float angle, bool useRadians = false)
        {
            // Convert the angle to radians if needed
            if (useRadians)
            {
                angle = angle.toRadians();
            }

            // Calculate the direction using trigonometric functions
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            return new Vector3(x, 0, z);
        }

        public static float GetAngleToFront180(this Vector3 FromGlobalPosition, Node3D Target, bool useRadians = false)
        {
            return FloatExtension.GetAngleToFront180(
                GetYRotation(FromGlobalPosition - Target.GlobalPosition, useRadians),
                useRadians ? Target.Rotation.Y : Target.RotationDegrees.Y,
                useRadians
                );
        }
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

        public static bool TryIntersectFlat(this Vector3 A, Vector3 a, Vector3 B, Vector3 b, out Vector3 C, bool allowInfiniteDistance = true)
        {
            var result = A.toXY().TryIntersect(a.toXY(), B.toXY(), b.toXY(), out var c);

            C = c.toXZ();

            if (result && allowInfiniteDistance == false)
            {
                if (C.DistanceTo(A) > a.Length() || C.DistanceTo(B) > b.Length())
                    result = false;
            }

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