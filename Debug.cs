using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public static class Debug
    {
        #region Rendering 3D Functions
        public static MeshInstance3D Point(this Node node, Vector3 point, Color color, float radius = .05f) => Render.DrawPoint(node, point, color, radius);

        public static MeshInstance3D Line(this Node node, Vector3 from, Vector3 to, Color color) => Render.DrawLine(node, new List<Vector3>() { from, to }, color);
        public static MeshInstance3D Ray(this Node node, Vector3 source, Vector3 dir, Color color) => Render.DrawRay(node, source, dir, color);
        public static MeshInstance3D Line(this Node node, List<Vector3> path, Color color) => Render.DrawLine(node, path, color);
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

        public static void Throw(this string message) => throw new System.Exception(message);
        #endregion
    }
}