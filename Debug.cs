using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public static class Debug
    {
        #region Rendering 3D Functions
        public static MeshInstance3D DrawRay(this Node node, Vector3 source, Vector3 dir, Color color) => DrawLine(node, new List<Vector3>() { source, source + dir }, color);
        public static MeshInstance3D DrawLine(this Node node, Vector3 from, Vector3 to, Color color) => DrawLine(node, new List<Vector3>() { from, to }, color);
        public static MeshInstance3D DrawLine(this Node node, List<Vector3> points, Color color)
        {
            if (color.A <= 0 || points.IsEmpty())
            {
                Debug.LogError($"CannotDrawLine: No points have been given");
                return null;
            }

            if (points.Count < 2) return DrawPoint(node, points[0], color);

            MeshInstance3D mesh_instance = new MeshInstance3D();
            ImmediateMesh immediate_mesh = new ImmediateMesh();
            OrmMaterial3D material = new OrmMaterial3D();

            mesh_instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            mesh_instance.Mesh = immediate_mesh;

            immediate_mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
            for (int i = 1; i < points.Count; i++)
            {
                immediate_mesh.SurfaceAddVertex(points[i - 1]);
                immediate_mesh.SurfaceAddVertex(points[i]);
            }
            immediate_mesh.SurfaceEnd();

            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.AlbedoColor = color;

            node.AddChild(mesh_instance);
            return mesh_instance;
        }

        public static MeshInstance3D DrawPoint(this Node node, Vector3 point, Color color, float radius = .05f)
        {
            MeshInstance3D mesh_instance = new MeshInstance3D();
            OrmMaterial3D material = new OrmMaterial3D();
            SphereMesh sphere_mesh = new SphereMesh();

            mesh_instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            mesh_instance.Mesh = sphere_mesh;
            mesh_instance.Position = point;

            sphere_mesh.Radius = radius;
            sphere_mesh.Height = radius * 2;
            sphere_mesh.Material = material;

            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.AlbedoColor = color;

            node.AddChild(mesh_instance);
            return mesh_instance;
        }
        #endregion

        #region Logging
        public static void LogError(this object obj) => LogError(obj.ToString());
        public static void Log(this object obj) => Log(obj.ToString());

        public static void LogError(this string message) => GD.PrintErr(message);
        public static void Log(this string message) => GD.Print(message);

        public static void Log<T>(this T[] array, string name = "array")
        {
            string result = $"{name}: {'{'} ";

            if (array.NotEmpty())
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0) result += ',';
                    result += $" {array[i]}";
                }

            Log(result + " }");
        }
        #endregion
    }
}