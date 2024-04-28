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

        public static float CenterDistance(Vector2 position, params Vector2[] corners)
        => position.DistanceTo(Vector2f.Average(corners));

        public static float EdgeDistance(Vector2 position, params Vector2[] corners) => EdgeDistance(position, out _, corners);
        public static float EdgeDistance(Vector2 position, out int edgeIndex, params Vector2[] corners)
        {
            var max = edge(0);
            edgeIndex = 0;

            for (int i = 1; i < corners.Length; i++)
            {
                var e = edge(i);

                if (e > max)
                {
                    edgeIndex = i;
                    max = e;
                }
            }

            return Mathf.Max(0, max);

            float edge(int i)
            {
                var a = corners[++i % corners.Length];
                var b = corners[--i];

                var d = (b - a).Normalized();
                var north = (position - b).Dot(d);
                var south = (position - a).Dot(-d);

                return Floatf.Max((position - a).Dot(d.RotatedD(90)), north * Core.GoldenCut, south * Core.GoldenCut);
            }
        }
    }
}