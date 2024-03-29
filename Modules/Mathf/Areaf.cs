using Godot;

namespace Cutulu
{
    public static class Areaf
    {
        public static float GetAreaInM2(this Vector2 a, Vector2 b, Vector2 c)
        {
            // Calculate two vectors representing two sides of the triangle
            Vector2 side1 = b - a;
            Vector2 side2 = c - a;

            // Calculate the cross product of the two sides
            float crossProduct = Vector2f.Cross(side1, side2);

            // Area of the triangle is half of the magnitude of the cross product
            float area = Mathf.Abs(crossProduct) * 0.5f;

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