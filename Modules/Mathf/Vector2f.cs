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
    }
}