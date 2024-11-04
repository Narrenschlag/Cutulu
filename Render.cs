namespace Cutulu
{
    using Godot;

    public static class Renderf
    {
        private static OrmMaterial3D vertexMaterial;
        public static OrmMaterial3D VertexMaterial
        {
            get
            {
                if (vertexMaterial.IsNull())
                {
                    vertexMaterial = new()
                    {
                        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                        VertexColorUseAsAlbedo = true,
                    };
                }

                return vertexMaterial;
            }
        }

        private static OrmMaterial3D vertexMaterialAlpha;
        public static OrmMaterial3D VertexMaterialAlpha
        {
            get
            {
                if (vertexMaterialAlpha.IsNull())
                {
                    vertexMaterialAlpha = new()
                    {
                        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                        VertexColorUseAsAlbedo = true,
                    };
                }

                return vertexMaterialAlpha;
            }
        }

        #region Base Functions		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static MeshInstance3D StartLineMesh(this Color color, out ImmediateMesh mesh)
        {
            MeshInstance3D mesh_instance = new();
            OrmMaterial3D material = new();
            mesh = new ImmediateMesh();

            mesh_instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            mesh_instance.Mesh = mesh;

            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.AlbedoColor = color;

            mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
            return mesh_instance;
        }

        public static void EndLineMesh(this ImmediateMesh mesh)
        {
            mesh.SurfaceEnd();
        }

        public static MeshInstance3D DrawLine(this Node parent, Color color, params Vector3[] points)
        {
            if (points.Size() < 2) return default;

            var tool = Mesh.PrimitiveType.LineStrip.Open();

            for (int i = 0; i < points.Length; i++)
            {
                tool.SetColor(color);
                tool.AddVertex(points[i]);
            }

            var mesh = new MeshInstance3D()
            {
                MaterialOverride = VertexMaterial,
                Mesh = tool.Commit(),
                Name = "Line",
            };

            parent.AddChild(mesh);
            mesh.GlobalPosition = Vector3.Zero;

            return mesh;
        }

        public static MeshInstance3D DrawPoint(this Node node, Vector3 position, Color color, float radius = .05f)
        {
            MeshInstance3D mesh_instance = new();
            OrmMaterial3D material = new();
            SphereMesh sphere_mesh = new();

            mesh_instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            mesh_instance.Mesh = sphere_mesh;

            sphere_mesh.Material = material;
            sphere_mesh.Height = radius * 2;
            sphere_mesh.Radius = radius;

            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.AlbedoColor = color;

            node.AddChild(mesh_instance);
            mesh_instance.GlobalPosition = position;

            return mesh_instance;
        }
        #endregion

        #region Line Functions		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static MeshInstance3D DrawRay(this Node node, Color color, Vector3 source, Vector3 direction) => DrawLine(node, color, new Vector3[2] { source, source + direction });
        public static MeshInstance3D DrawLine(this Node node, Color color, Vector3 from, Vector3 to) => DrawLine(node, color, new Vector3[2] { from, to });
        #endregion

        #region Curve Functions		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static MeshInstance3D DrawCurve(Vector3 origin, Vector3 direction, Color color, float gravity, float resolution, float length)
            => DrawCurve(Core.Main3D, origin, direction, color, gravity, resolution, length);

        public static MeshInstance3D DrawCurve(this Node3D node, Color color, float gravity, float resolution, float length)
            => DrawCurve(node, node.GlobalPosition, node.Forward(), color, gravity, resolution, length);

        public static MeshInstance3D DrawCurve(this Node node, Vector3 origin, Vector3 direction, Color color, float gravity, float resolution, float length)
        {
            if (length <= 0) return null;

            int count = Mathf.FloorToInt(length / resolution);
            float rest = length - count * resolution;
            if (count < 1 && rest <= 0) return null;

            MeshInstance3D mesh_instance = StartLineMesh(color, out ImmediateMesh mesh);
            Vector3 right = direction.toRight();
            Vector3 last = origin;
            gravity *= resolution;

            // Draw main part
            for (int i = 0; i < count; i++) add();

            // Draw rest
            if (rest > 0) add(rest / resolution);

            // Return result
            mesh.EndLineMesh();
            node.AddChild(mesh_instance);
            return mesh_instance;

            void rotate() => direction = direction.Rotated(right, gravity);
            void add(float value = 1f)
            {
                mesh.SurfaceAddVertex(last);

                rotate();
                last += direction * value * resolution;
                mesh.SurfaceAddVertex(last);
            }
        }
        #endregion

        #region Visual Layers

        public static bool GetLayer(this VisualInstance3D vis, int i) => new BitBuilder(vis.Layers)[i];
        public static bool GetLayer(this Camera3D vis, int i) => new BitBuilder(vis.CullMask)[i];
        public static bool GetLayer(this CanvasItem vis, int i) => new BitBuilder(vis.VisibilityLayer)[i];
        public static bool GetLayer(this Camera2D vis, int i) => new BitBuilder(vis.VisibilityLayer)[i];

        public static void SetLayer(this VisualInstance3D vis, int i, bool value) => vis.Layers = new BitBuilder(vis.Layers) { [i] = value, }.ByteBuffer.Decode<uint>();
        public static void SetLayer(this Camera3D vis, int i, bool value) => vis.CullMask = new BitBuilder(vis.CullMask) { [i] = value, }.ByteBuffer.Decode<uint>();
        public static void SetLayer(this CanvasItem vis, int i, bool value) => vis.VisibilityLayer = new BitBuilder(vis.VisibilityLayer) { [i] = value, }.ByteBuffer.Decode<uint>();
        public static void SetLayer(this Camera2D vis, int i, bool value) => vis.VisibilityLayer = new BitBuilder(vis.VisibilityLayer) { [i] = value, }.ByteBuffer.Decode<uint>();

        public static void SetLayers(this VisualInstance3D vis, bool value) => vis.Layers = value ? uint.MaxValue : 0;
        public static void SetLayers(this Camera3D vis, bool value) => vis.CullMask = value ? uint.MaxValue : 0;
        public static void SetLayers(this CanvasItem vis, bool value) => vis.VisibilityLayer = value ? uint.MaxValue : 0;
        public static void SetLayers(this Camera2D vis, bool value) => vis.VisibilityLayer = value ? uint.MaxValue : 0;

        #endregion
    }
}
