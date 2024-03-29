using Godot;

namespace Cutulu
{
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
    }
}