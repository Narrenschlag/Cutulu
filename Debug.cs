using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public static class Debug
    {
        #region Rendering 3D Functions  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Draws a point in 3d space.
        /// </summary>
        public static MeshInstance3D Point(this Node node, Vector3 point, Color color, float radius = .05f) => Render.DrawPoint(node, point, color, radius);

        /// <summary>
        /// Draws a ray in 3d space.
        /// </summary>
        public static MeshInstance3D Ray(this Node node, Vector3 source, Vector3 dir, Color color) => Render.DrawRay(node, source, dir, color);

        /// <summary>
        /// Draws a line in 3d space.
        /// </summary>
        public static MeshInstance3D Line(this Node node, Vector3 from, Vector3 to, Color color) => Render.DrawLine(node, new List<Vector3>() { from, to }, color);

        /// <summary>
        /// Draws a line in 3d space.
        /// </summary>
        public static MeshInstance3D Line(this Node node, List<Vector3> path, Color color) => Render.DrawLine(node, path, color);
        #endregion

        #region Logging                 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(this object obj) => LogError(obj.ToString());

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(this string message) => GD.PrintErr(message);

        /// <summary>
        /// Logs a warning console message.
        /// </summary>
        public static void LogWarning(this string message) => GD.PushWarning(message);

        /// <summary>
        /// Logs a default console message.
        /// </summary>
        public static void Log(this object obj) => Log(obj.ToString());

        /// <summary>
        /// Logs a default console message.
        /// </summary>
        public static void Log(this string message) => GD.Print(message);

        /// <summary>
        /// Logs a default console message. Message is formatted using bbcode.
        /// </summary>
        public static void LogR(this string message) => GD.PrintRich(message);

        /// <summary>
        /// Logs a default console message.
        /// </summary>
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

        /// <summary>
        /// Throws an error message and stop code from continuing.
        /// </summary>
        public static void Throw(this string message) => throw new System.Exception(message);
        #endregion
    }
}