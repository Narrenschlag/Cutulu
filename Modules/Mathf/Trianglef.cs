using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public static class Trianglef
    {
        public static Vector2[] OrderClockwise(Vector2 a, Vector2 b, Vector2 c)
        => OrderClockwise(ref a, ref b, ref c);

        public static Vector2[] OrderClockwise(ref Vector2 a, ref Vector2 b, ref Vector2 c)
        {
            var points = new List<Vector2> { a, b, c };
            var referencePoint = (a + b + c) / 3f; // You can choose any point as reference

            points.Sort((p1, p2) => CompareAngles(ref referencePoint, ref p1, ref p2));
            return points.ToArray();

            static int CompareAngles(ref Vector2 referencePoint, ref Vector2 a, ref Vector2 b)
            {
                // Calculate angles between vectors
                var angleA = Mathf.Atan2(a.Y - referencePoint.Y, a.X - referencePoint.X);
                var angleB = Mathf.Atan2(b.Y - referencePoint.Y, b.X - referencePoint.X);

                // Ensure angles are between 0 and 2*Pi
                if (angleA < 0) angleA += 2 * Mathf.Pi;
                if (angleB < 0) angleB += 2 * Mathf.Pi;

                // Compare angles
                return angleA.CompareTo(angleB);
            }
        }

        public static float CenterDistance(this Vector2 position, params Vector2[] corners)
        => position.DistanceTo(Vector2Extension.Average(corners));

        public static float EdgeDistance(this Vector2 position, params Vector2[] corners) => EdgeDistance(position, out _, corners);
        public static float EdgeDistance(this Vector2 position, Vector2 a, Vector2 b)
        {
            // Calculate the direction vector from v1 to v2
            var dv = b - a;

            // Calculate the vector from v1 to the given point p
            var vp = position - a;

            // Project the vector from v1 to p onto the edge (v1, v2)
            float t = vp.Dot(dv) / dv.Dot(dv);

            // Clamp t to ensure it's within the bounds of the edge
            t = Mathf.Min(Mathf.Max(t, 0f), 1f);

            // Calculate the closest point on the edge using the projected t value
            return position.DistanceTo(a + t * dv);
        }

        public static float EdgeDistance(this Vector2 position, out int edgeIndex, params Vector2[] corners)
        {
            var min = EdgeDistance(position, corners[0], corners[^1]);
            edgeIndex = corners.Length - 1;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                var distance = EdgeDistance(position, corners[i], corners[i + 1]);
                if (distance < min)
                {
                    min = distance;
                    edgeIndex = i;
                }
            }

            return Mathf.Max(0, min);
        }

        public static Vector3 RayToY(this Vector3 origin, Vector3 direction, float y = 0)
        => origin.IntersectAt(direction, y);

        public static float FindYOnTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 p)
        {
            // Step 1: Calculate two vectors from the triangle points
            var AB = B - A;
            var AC = C - A;

            // Step 2: Compute the normal of the triangle's plane
            var normal = AB.Cross(AC);

            // Step 3: Plane equation is: Ax + By + Cz + D = 0
            // Where (A, B, C) are the components of the normal vector
            var D = -normal.Dot(A); // Compute D using point A

            // Step 4: Solve for Y in the plane equation Ax + By + Cz + D = 0
            // Given that A = normal.x, B = normal.y, C = normal.z:
            // normal.X * p.X + normal.Y * p.Y + normal.Z * p.Z + D = 0
            // Solve for p.Y: p.Y = (-normal.X * p.X - normal.Z * p.Z - D) / normal.Y
            return (-normal.X * p.X - normal.Z * p.Z - D) / normal.Y;
        }
    }
}