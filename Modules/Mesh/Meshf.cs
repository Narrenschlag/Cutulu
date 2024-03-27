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
    }
}