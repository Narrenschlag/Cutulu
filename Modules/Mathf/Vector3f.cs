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
    }
}