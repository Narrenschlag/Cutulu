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
    }
}