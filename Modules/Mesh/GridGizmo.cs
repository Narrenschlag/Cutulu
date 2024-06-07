using System.Threading.Tasks;
using Godot;

namespace Cutulu
{
    public partial class GridGizmo : MeshInstance3D
    {
        public GridGizmo(Node parent, string name, Material material = null) : base()
        {
            Name = $"Gizmo {name.Trim()}";

            parent.AddChild(this);
            GlobalPosition = Vector3.Up * .001f;

            if (material.NotNull()) MaterialOverride = material;
            else MaterialOverride = new StandardMaterial3D()
            {
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                VertexColorUseAsAlbedo = true
            };
        }

        private float cellSize = 1f;
        public float CellSize
        {
            get => cellSize;
            set
            {
                if (value == cellSize) return;

                cellSize = value;
                GenerateGrid();
            }
        }

        private Color color = Colors.White;
        public Color Color
        {
            get => color;
            set
            {
                if (value == color) return;

                color = value;
                GenerateGrid();
            }
        }

        private float colorFalloff = 0f;
        public float ColorFalloff
        {
            get => colorFalloff;
            set
            {
                if (colorFalloff == value) return;

                colorFalloff = value;
                GenerateGrid();
            }
        }

        private float radius = 10f;
        public float Radius
        {
            get => radius;
            set
            {
                if (radius == value) return;

                radius = value;
                GenerateGrid();
            }
        }

        private byte queue = 0;
        public async void GenerateGrid()
        {
            queue++;
            await Task.Delay(1);
            if (--queue > 0) return;
            queue = 0;

            lock (this)
            {
                if (Mesh.NotNull()) Mesh.Dispose();

                if (CellSize <= 0 || Color.A <= 0) return;

                // Create a SurfaceTool
                var surfaceTool = new SurfaceTool();
                surfaceTool.Begin(Mesh.PrimitiveType.Lines);

                int amount = Mathf.CeilToInt(Radius / CellSize * 0.5f);
                draw(Vector3.Forward, Vector3.Right);
                draw(Vector3.Right, Vector3.Forward);

                void draw(Vector3 forward, Vector3 right)
                {
                    for (int i = 0; i < amount; i++)
                    {
                        var max = 1f - Vector2.Zero.DistanceTo(new Vector2((float)i / amount, 1f)) / Mathf.Pi;
                        var falloff = ColorFalloff * max;

                        for (int k = i > 0 ? -1 : 1; k < 2; k += 2)
                        {
                            if (ColorFalloff > 0) draw(-falloff, falloff, false);
                            draw(-falloff, -max, true);
                            draw(falloff, max, true);

                            void draw(float a, float b, bool endsTransparent)
                            {
                                surfaceTool.SetColor(Color);
                                surfaceTool.AddVertex(right * i * k * CellSize + forward * Radius * a);

                                surfaceTool.SetColor(endsTransparent ? Colors.Transparent : Color);
                                surfaceTool.AddVertex(right * i * k * CellSize + forward * Radius * b);
                            }
                        }
                    }
                }

                // Create a MeshInstance and add the mesh
                // Generate the mesh
                Mesh = surfaceTool.Commit();
            }
        }
    }
}