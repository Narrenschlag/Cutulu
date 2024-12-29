namespace Cutulu.Mesh
{
    using Godot;

    using Core;

    public static class Meshf
    {
        public static Mesh Create(this Mesh.PrimitiveType type, params (Vector3, Color)[] vertices) => Open(type, vertices).Commit();

        public static SurfaceTool Open(this Mesh.PrimitiveType type, params (Vector3, Color)[] vertices)
        {
            var surfaceTool = new SurfaceTool();
            surfaceTool.Begin(type);

            for (int i = 0; i < vertices.Length; i++)
            {
                Add(surfaceTool, vertices[i].Item1, vertices[i].Item2);
            }

            return surfaceTool;
        }

        public static SurfaceTool Open(this Mesh.PrimitiveType type, Color color, params Vector3[] vertices)
        {
            var surfaceTool = new SurfaceTool();
            surfaceTool.Begin(type);

            for (int i = 0; i < vertices.Length; i++)
            {
                Add(surfaceTool, vertices[i], color);
            }

            return surfaceTool;
        }

        public static void Add(this SurfaceTool surfaceTool, Vector3 position, Color color)
        {
            surfaceTool.SetColor(color);
            surfaceTool.AddVertex(position);
        }

        public static void Apply(this SurfaceTool surfaceTool, MeshInstance3D meshInstance3D)
        {
            if (meshInstance3D.IsNull()) return;

            if (meshInstance3D.Mesh.NotNull()) meshInstance3D.Mesh.Dispose();
            meshInstance3D.Mesh = surfaceTool.Commit();
        }

        public static void AddTriangle(this SurfaceTool surfaceTool, Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Color color, bool clockwise, ref int vertexOffset)
        {
            var min = Vector3f.Min(a, b, c);
            var max = Vector3f.Max(a, b, c);

            Vector2 getUv(ref Vector3 input) => (input - min).toXY() / max.toXY();
            var uvs = new Vector2[3] { getUv(ref a), getUv(ref b), getUv(ref c) };

            var vertices = new Vector3[3] { a, b, c };
            for (byte i = 0; i < 3; i++)
            {
                surfaceTool.SetNormal(normal);
                surfaceTool.SetColor(color);
                surfaceTool.SetUV(uvs[i]);

                surfaceTool.AddVertex(vertices[i]);
            }

            for (byte i = (byte)(clockwise ? 0 : 2), k = 0; k < 3; next(ref i), k++) surfaceTool.AddIndex(vertexOffset + i % 3); // First triangle
            void next(ref byte i) { if (clockwise) i++; else i--; }

            vertexOffset += 3;
        }

        public static void AddPlane(this SurfaceTool surfaceTool, Vector3 a, Vector3 b, Vector3 delta, Color color, bool clockwise, ref int vertexOffset)
            => AddPlane(
                surfaceTool, a, b, b + delta, a + delta,
                (b - a).Normalized().toRight() * (clockwise ? -1 : 1),
                color, clockwise, ref vertexOffset
            );

        public static void AddPlane(this SurfaceTool surfaceTool, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color, bool clockwise, ref int vertexOffset)
        {
            var uvs = new Vector2[4] { Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Up };
            var vertices = new Vector3[4] { a, b, c, d };

            for (byte i = 0; i < 4; i++)
            {
                surfaceTool.SetNormal(normal);
                surfaceTool.SetColor(color);
                surfaceTool.SetUV(uvs[i]);

                surfaceTool.AddVertex(vertices[i]);
            }

            for (byte i = (byte)(clockwise ? 0 : 2), k = 0; k < 3; next(ref i), k++) surfaceTool.AddIndex(vertexOffset + i % 4); // First triangle
            for (byte i = (byte)(clockwise ? 2 : 0), k = 0; k < 3; next(ref i), k++) surfaceTool.AddIndex(vertexOffset + i % 4); // Second triangle
            void next(ref byte i) { if (clockwise) i++; else i--; }

            vertexOffset += 4;
        }

        public static void AddPlane(this SurfaceTool surfaceTool, Vector3 a, Vector3 b, Vector3 normal, Color color, bool clockwise, ref int vertexCount, params Vector4[] cuts)
        {
            if (cuts.IsEmpty()) surfaceTool.AddPlane(a, b, normal, color, clockwise, ref vertexCount);

            else
            {
                if (cuts[0].X > 0) surfaceTool.AddPlane(a, a.Lerp(b, cuts[0].X), normal, color, clockwise, ref vertexCount);

                for (int i = 0; i < cuts.Length; i++)
                {
                    // Draw bot
                    if (cuts[i].Z > 0) surfaceTool.AddPlane(a.Lerp(b, cuts[i].X), a.Lerp(b, Mathf.Clamp(cuts[i].Y, 0f, 1f)), normal * cuts[i].Z, color, clockwise, ref vertexCount);

                    // Draw top
                    if (cuts[i].W > 0)
                    {
                        var height = normal.Y * cuts[i].W;
                        var y = normal.Normalized() * (normal.Y - height);

                        surfaceTool.AddPlane(a.Lerp(b, cuts[i].X) + y, a.Lerp(b, Mathf.Clamp(cuts[i].Y, 0f, 1f)) + y, normal.Normalized() * height, color, clockwise, ref vertexCount);
                    }

                    // Draw follow up
                    if (cuts[i].Y < 1f)
                    {
                        if (i < cuts.Length - 1) surfaceTool.AddPlane(a.Lerp(b, cuts[i].Y), a.Lerp(b, cuts[i + 1].X), normal, color, clockwise, ref vertexCount);
                        else surfaceTool.AddPlane(a.Lerp(b, cuts[i].Y), b, normal, color, clockwise, ref vertexCount);
                    }
                }
            }
        }
    }
}