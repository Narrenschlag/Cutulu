using Godot;

namespace Cutulu.Core
{
    /// <summary>
    /// Mathf extension for area calculations.
    /// </summary>
    public static class Areaf
    {
        public static float GetAreaInM2(this Vector2 a, Vector2 b, Vector2 c)
        {
            // Calculate two vectors representing two sides of the triangle
            var side1 = b - a;
            var side2 = c - a;

            // Calculate the cross product of the two sides
            var crossProduct = Vector2f.Cross(side1, side2);

            // Area of the triangle is half of the magnitude of the cross product
            var area = Mathf.Abs(crossProduct) * 0.5f;

            return area;
        }

        public static float GetAreaInM2(this Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var area = (float)Mathf.Abs(0.5 * (
                a.X * b.Y + b.X * c.Y + c.X * d.Y + d.X * a.Y -
                b.X * a.Y - c.X * b.Y - d.X * c.Y - a.X * d.Y
            ));

            return area;
        }
    }
}