namespace Cutulu
{
    using Godot;

    public static class GodotObjectExtension
    {
        public static bool IsNull(this GodotObject node) => node == null || !GodotObject.IsInstanceValid(node);
        public static bool NotNull(this GodotObject node) => !IsNull(node);
    }
}