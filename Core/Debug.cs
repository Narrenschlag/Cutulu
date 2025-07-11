namespace Cutulu.Core
{
    using System.Collections.Generic;

#if GODOT4_0_OR_GREATER
    using Godot;
#endif

    public static class Debug
    {
        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(this object obj) => LogError(obj.ToString());

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(this string message)
        {
#if GODOT4_0_OR_GREATER
            GD.PrintErr(message);
#else
            Log($"[ERROR] {message}");
#endif
        }

        /// <summary>
        /// Logs a warning console message.
        /// </summary>
        public static void LogWarning(this string message)
        {
#if GODOT4_0_OR_GREATER
            GD.PushWarning(message);
#else
            Log($"[WARNING] {message}");
#endif
        }

        /// <summary>
        /// Logs a default console message.
        /// </summary>
        public static void Log(this string message)
        {
#if GODOT4_0_OR_GREATER
            GD.Print(message);
#else
            Console.WriteLine(message);
#endif
        }

        /// <summary>
        /// Logs a default console message. Message is formatted using bbcode.
        /// </summary>
        public static void LogR(this string message)
        {
#if GODOT4_0_OR_GREATER
            GD.PrintRich(message);
#else
            Log($"[RICH] {message}");
#endif
        }

#if GODOT4_0_OR_GREATER
        /// <summary>
        /// Logs a default console message. Message is formatted using bbcode.
        /// </summary>
        public static void LogR<T>(this string message, Color color) => LogR($"[b][color={color.ToHtml()}][{typeof(T).Name}][/color][/b] {message}");
#endif

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

#if GODOT4_0_OR_GREATER
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
#endif
    }
}