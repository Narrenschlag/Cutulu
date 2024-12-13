namespace Cutulu
{
    using Godot;

    public static class GodotObjectExtension
    {
        public static bool IsNull(this GodotObject obj) => obj == null || !GodotObject.IsInstanceValid(obj);
        public static bool NotNull(this GodotObject obj) => !IsNull(obj);

        public static bool Destroy(this GodotObject obj)
        {
            if (obj.IsNull()) return false;

            obj.Dispose();
            return true;
        }
    }
}