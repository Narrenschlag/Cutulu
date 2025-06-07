namespace Cutulu.Core
{
    using System.Collections.Generic;
    using Godot;

    public static class Debug
    {
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
        public static void Log(this string message) => GD.Print(message);

        /// <summary>
        /// Logs a default console message. Message is formatted using bbcode.
        /// </summary>
        public static void LogR(this string message) => GD.PrintRich(message);

        /// <summary>
        /// Logs a default console message. Message is formatted using bbcode.
        /// </summary>
        public static void LogR<T>(this string message, Color color) => GD.PrintRich($"[b][color={color.ToHtml()}][{typeof(T).Name}][/color][/b] {message}");

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

        public static void LogHierarchie(this Node obj)
        {
            var elements = new List<string>() { obj.Name };

            while ((obj = obj.GetParent()).NotNull())
            {
                elements.Add(obj.Name);
            }

            elements.Reverse();
            Log(string.Join(" -> ", elements));
        }
    }
}