using Godot;

namespace Cutulu
{
    public partial class GridGizmo : MeshInstance3D
    {
        public GridGizmo(Node parent, string name, Color mainColor, Color subColor, byte gridSize = 16, byte resolution = 4) : base()
        {
            Name = $"Gizmo_{name.Trim()}";
            parent.AddChild(this);

            GlobalPosition = Vector3.Up * .001f;
            GenerateGrid(gridSize, resolution, mainColor, subColor);
        }

        public void GenerateGrid(byte size, byte resolution, Color mainColor, Color subColor)
        {
            // Prepare transform
            Vector3 oldPosition = GlobalPosition;
            Vector3 oldRotation = GlobalRotation;
            GlobalPosition = Vector3.Zero;
            GlobalRotation = Vector3.Zero;

            // Create a SurfaceTool
            var surfaceTool = new SurfaceTool();
            surfaceTool.Begin(Mesh.PrimitiveType.Lines);

            float res = 1f / resolution;
            float len = size * 2;

            // Background Grid
            if (res < 1)
            {
                enumerate(Vector3.Right, Vector3.Forward, size * resolution, len, res, resolution, subColor);
                enumerate(Vector3.Forward, Vector3.Right, size * resolution, len, res, resolution, subColor);
            }

            // Main Grid
            enumerate(Vector3.Right, Vector3.Forward, size, len, 1f, 0, mainColor, .001f);
            enumerate(Vector3.Forward, Vector3.Right, size, len, 1f, 0, mainColor, .001f);

            void enumerate(Vector3 forward, Vector3 right, int size, float length, float factor, float skipModul, Color color, float yOffset = 0)
            {
                for (int i = 1 - size, k = 0; i < size; i++, k++)
                {
                    // Ignore
                    if (skipModul > 0 && i % skipModul == 0) continue;

                    // Draw line
                    add(forward * i * factor, right, length, color, color, yOffset);
                }
            }

            // Defines the vertecies
            void add(Vector3 center, Vector3 forward, float length, Color startColor, Color endColor, float yOffset)
            {
                surfaceTool.SetColor(startColor);
                surfaceTool.AddVertex(center += -forward * length * .5f + Vector3.Up * yOffset);

                surfaceTool.SetColor(endColor);
                surfaceTool.AddVertex(center + forward * length);
            }

            // Dispose old mesh
            if (Mesh.NotNull()) Mesh.Dispose();

            // Create a MeshInstance and add the mesh
            // Generate the mesh
            Mesh = surfaceTool.Commit();

            // Revert transform
            GlobalPosition = oldPosition;
            GlobalRotation = oldRotation;
        }
    }
}